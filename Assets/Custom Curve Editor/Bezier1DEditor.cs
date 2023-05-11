using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using Sirenix.Utilities;
using UnityEditor.PackageManager.UI;
using System.Runtime.CompilerServices;
using UnityEditorInternal;
using UnityEngine.SocialPlatforms;
using System.Threading;

public class Bezier1DEditor : EditorWindow
{
    public static Bezier1DEditor window;

    public BezierCurve1D curve;
    public List<SegmentBinding> segmentBindings;
    public List<Rect> handles;
    public List<Rect> controls;
    private Texture2D _white, _blue;
    private float handleSizeX = 20f;
    private float handleSizeY = 20f;
    private float controlScalar = .9f;
    private float curveYOffset = 100f;
    private float doubleClickWindow = 0.5f;
    private DateTime lastClickTime;
    private Vector2 canvasSize = new Vector2(1000f, 400f);
    private DragInfo dragHandle;
    private SelectedSegmentBinding selectedSegment;
    private bool shiftHeld = false;
    private List<CurveRegionColor> curveColors;

    public class SegmentBinding
    {
        public int segmentId = -1;
        public int leftHandleId = -1;
        public int rightHandleId = -1;
        public int leftControlId = -1;
        public int rightControlId = -1;
        public Keyframe1D curve;
    }

    public class SelectedSegmentBinding
    {
        public SegmentBinding segment;
        public string leftHandleValueX;
        public string leftHandleValueY;
        public string rightHandleValueX;
        public string rightHandleValueY;
        public string leftControlValue;
        public string rightControlValue;
        public CurveRegionColor colorRegion;
    }

    public class DragInfo
    {
        public bool isHandle;
        public List<Rect> list;
        public int id;
    }

    public class CurveRegionColor
    {
        public Vector2 range;
        public Color color;
    }

    [MenuItem("Tools/Bezier Editor Fixed")]
    public static void Init()
    {
        window = EditorWindow.GetWindow<Bezier1DEditor>();
        window.titleContent = new GUIContent("Curve editor");
        window.ShowNodes();
    }

    public void InitInstance()
    {
        Init();
    }

    public void ShowNodes()
    {
        //Resize draw regions based on window size
        canvasSize.x = position.width;
        curveYOffset = position.height / 4f;
        canvasSize.y = position.height / 2f;

        //Temporary: Define base curve being drawn
        if(curve == null)
        {
            curve = new BezierCurve1D(0f, 0f, 1f, 1f);

            for (float t = 0.1f; t < 1f; t += 0.1f)
            {
                curve.DivideCurve(t);
            }
        }

        InitializeCurve();

        _white = AssetDatabase.LoadAssetAtPath("Assets/Custom Curve Editor/Icons/White.png", typeof(Texture2D)) as Texture2D;
        _blue = AssetDatabase.LoadAssetAtPath("Assets/Custom Curve Editor/Icons/Blue.png", typeof(Texture2D)) as Texture2D;
    }

    void OnGUI()
    {
        //Resize draw regions based on window size
        canvasSize.x = position.width;
        curveYOffset = position.height / 4f;
        canvasSize.y = position.height / 2f;
        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftShift)
        {
            shiftHeld = true;
        }
        else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftShift)
        {
            shiftHeld = false;
        }

        //Draw white square that represents [0,1]
        GUI.DrawTexture(new Rect(0, curveYOffset, canvasSize.x, canvasSize.y), _white);

        //Place and draw the window types
        BeginWindows();
        PlaceWindows(controls, false);
        EndWindows();

        BeginWindows();
        PlaceWindows(handles, true);
        EndWindows();

        if (dragHandle != null)
        {
            DragHandle();
        }

        HandleSelection();
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (selectedSegment != null)
        {
            Rect leftHandle = handles[selectedSegment.segment.leftHandleId];
            Rect rightHandle = handles[selectedSegment.segment.rightHandleId];
            Rect leftControl = controls[selectedSegment.segment.leftControlId];
            Rect rightControl = controls[selectedSegment.segment.rightControlId];

            bool isDragging = dragHandle != null;

            GUILayout.Label("Left Handle");
            selectedSegment.leftHandleValueX = GUILayout.TextField(isDragging ? leftHandle.x.ToString("0.00") : selectedSegment.leftHandleValueX);
            GUILayout.Label(",");
            selectedSegment.leftHandleValueY = GUILayout.TextField(isDragging ? leftHandle.y.ToString("0.00") : selectedSegment.leftHandleValueY);
            GUILayout.Label("Left Control");
            selectedSegment.leftControlValue = GUILayout.TextField(isDragging ? leftControl.y.ToString("0.00") : selectedSegment.leftControlValue);
            GUILayout.Label("Right Control");
            selectedSegment.rightControlValue = GUILayout.TextField(isDragging ? rightControl.y.ToString("0.00") : selectedSegment.rightControlValue);
            GUILayout.Label("Right Handle");
            selectedSegment.rightHandleValueX = GUILayout.TextField(isDragging ? rightHandle.x.ToString("0.00") : selectedSegment.rightHandleValueX);
            GUILayout.Label(",");
            selectedSegment.rightHandleValueY = GUILayout.TextField(isDragging ? rightHandle.y.ToString("0.00") : selectedSegment.rightHandleValueY);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        DrawNodeCurve();
    }

    private void DrawHandle(int id)
    {
        DrawWindow(id, true);
    }

    private void DrawControl(int id)
    {
        DrawWindow(id, false);
    }

    private void InitializeCurve()
    {
        //Create handle rectangles and bind them to paired curve segments by ID.
        curveColors = new List<CurveRegionColor>();
        segmentBindings = new List<SegmentBinding>();
        handles = new List<Rect>();
        controls = new List<Rect>();
        selectedSegment = null;
        dragHandle = null;

        for (int i = 0; i < curve.keys.Count; i++)
        {
            SegmentBinding binding = new SegmentBinding();
            segmentBindings.Add(binding);
            binding.segmentId = i;
            binding.leftHandleId = i;
            binding.rightHandleId = i + 1;
            binding.leftControlId = 2 * i;
            binding.rightControlId = (2 * i) + 1;
            Keyframe1D key = curve.keys[i];
            binding.curve = key;
            handles.Add(new Rect(key.range.x, (1f - key.a), handleSizeX, handleSizeY));
            controls.Add(new Rect(key.GetControlPosition(1), (1f - key.b), handleSizeX * controlScalar, handleSizeY * controlScalar));
            controls.Add(new Rect(key.GetControlPosition(2), (1f - key.c), handleSizeX * controlScalar, handleSizeY * controlScalar));

            if (i == curve.keys.Count - 1)
            {
                handles.Add(new Rect(key.range.y, (1f - key.d), handleSizeX, handleSizeY));
            }

        }
    }

    //function to highlight and select a curve segment.
    private void HandleSelection()
    {
        //Handle selecting a segment on click
        if (Event.current.type == EventType.MouseDown)
        {
            Select();
        }

        //Handle setting values from text fields if segement is selected
        if (selectedSegment != null)
        {
            //Right click / escape was pressed, deselect all segments
            if (Event.current.type == EventType.ContextClick ||
            (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                selectedSegment = null;
                curveColors.Clear();
                Repaint();
                return;
            }

            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
            {
                if (selectedSegment.segment.segmentId != 0)
                {
                    curve.Remove(selectedSegment.segment.curve);
                    InitializeCurve();
                    Select();
                    Repaint();
                    return;
                }
            }

            //Update handle/control values with inputted values when enter is hit
            float value;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (float.TryParse(selectedSegment.leftHandleValueX, out value))
                {
                    selectedSegment.leftHandleValueX = SetHandleValue(selectedSegment.segment.leftHandleId, value).x.ToString("0.00");
                }
                if (float.TryParse(selectedSegment.leftHandleValueY, out value))
                {
                    selectedSegment.leftHandleValueY = SetHandleValue(selectedSegment.segment.leftHandleId, float.NegativeInfinity, value).y.ToString("0.00");
                }
                if (float.TryParse(selectedSegment.rightHandleValueX, out value))
                {
                    selectedSegment.rightHandleValueX = SetHandleValue(selectedSegment.segment.rightHandleId, value).x.ToString("0.00");
                }
                if (float.TryParse(selectedSegment.rightHandleValueY, out value))
                {
                    selectedSegment.rightHandleValueY = SetHandleValue(selectedSegment.segment.rightHandleId, float.NegativeInfinity, value).y.ToString("0.00");
                }
                if (float.TryParse(selectedSegment.leftControlValue, out value))
                {
                    selectedSegment.leftControlValue = SetControlValue(selectedSegment.segment.leftControlId, value).y.ToString("0.00");
                }
                if (float.TryParse(selectedSegment.rightControlValue, out value))
                {
                    selectedSegment.rightControlValue = SetControlValue(selectedSegment.segment.rightControlId, value).y.ToString("0.00");
                }
            }

            //update curve region color for selected segment so the correct region stays green
            selectedSegment.colorRegion.range = selectedSegment.segment.curve.range;
        }
    }

    private void Select()
    {
        Vector2 normMousePos = new Vector2(Event.current.mousePosition.x / canvasSize.x, Event.current.mousePosition.y / canvasSize.y);

        //Don't register a selection if it's not within the middle region of the screen (so top/bottom can be used for UI).
        if (normMousePos.y < .5f || normMousePos.y > 1.5f)
            return;

        curveColors.Clear();
        for (int i = 0; i < handles.Count; i++)
        {
            if (normMousePos.x >= handles[i].x && normMousePos.x < handles[i + 1].x)
            {
                foreach (SegmentBinding binding in segmentBindings)
                {
                    if (binding.leftHandleId == i)
                    {
                        //Check if this was a double click; if so, split the curve at the mouse position
                        if (selectedSegment != null && selectedSegment.segment.segmentId == i)
                        {
                            TimeSpan timeSinceLastClick = DateTime.Now - lastClickTime;
                            if (timeSinceLastClick.TotalSeconds < doubleClickWindow)
                            {
                                curve.DivideCurve(normMousePos.x);
                                InitializeCurve();
                                Repaint();
                                return;
                            }
                            lastClickTime = DateTime.Now;
                        }
                        else
                        {
                            lastClickTime = DateTime.Now;
                        }

                        Rect leftHandle = handles[binding.leftHandleId];
                        Rect rightHandle = handles[binding.rightHandleId];
                        Rect leftControl = controls[binding.leftControlId];
                        Rect rightControl = controls[binding.rightControlId];
                        selectedSegment = new SelectedSegmentBinding()
                        {
                            segment = binding,
                            leftHandleValueX = leftHandle.x.ToString("0.00"),
                            leftHandleValueY = leftHandle.y.ToString("0.00"),
                            leftControlValue = leftControl.y.ToString("0.00"),
                            rightControlValue = rightControl.y.ToString("0.00"),
                            rightHandleValueX = rightHandle.x.ToString("0.00"),
                            rightHandleValueY = rightHandle.y.ToString("0.00")
                        };
                        break;
                    }
                }
                CurveRegionColor crc = new CurveRegionColor
                {
                    range = new Vector2(handles[i].x, handles[i + 1].x),
                    color = Color.red
                };
                selectedSegment.colorRegion = crc;
                curveColors.Add(crc);
                Repaint();
                break;
            }
        }
    }

    private Rect SetHandleValue(int handleID, float valueX = float.NegativeInfinity, float valueY = float.NegativeInfinity)
    {
        SegmentBinding rightHandleSegment = null;
        SegmentBinding leftHandleSegment = null;

        foreach (SegmentBinding binding in segmentBindings)
        {
            if (binding.leftHandleId == handleID)
            {
                leftHandleSegment = binding;
            }
            if (binding.rightHandleId == handleID)
            {
                rightHandleSegment = binding;
            }
        }

        if (leftHandleSegment == null || rightHandleSegment == null)
        {
            valueX = float.NegativeInfinity;
        }

        Rect oldHandle = handles[handleID];
        if (valueX != float.NegativeInfinity)
        {
            Vector2 range;
            range.x = rightHandleSegment.curve.range.x + 0.015f;
            range.y = leftHandleSegment.curve.range.y - 0.015f;
            valueX = Mathf.Clamp(valueX, range.x, range.y);
            oldHandle.x = valueX;

            //Adjust ranges of bound curve segments to match new handle position
            leftHandleSegment.curve.range.x = valueX;
            rightHandleSegment.curve.range.y = valueX;

            //Adjust controls to stay 1/3rd and 2/3rd of the way between the respective handles
            Rect leftControl1 = controls[leftHandleSegment.leftControlId];
            Rect leftControl2 = controls[leftHandleSegment.rightControlId];
            Rect rightControl1 = controls[rightHandleSegment.leftControlId];
            Rect rightControl2 = controls[rightHandleSegment.rightControlId];

            leftControl1.x = leftHandleSegment.curve.GetControlPosition(1);
            leftControl2.x = leftHandleSegment.curve.GetControlPosition(2);
            rightControl1.x = rightHandleSegment.curve.GetControlPosition(1);
            rightControl2.x = rightHandleSegment.curve.GetControlPosition(2);

            controls[leftHandleSegment.leftControlId] = leftControl1;
            controls[leftHandleSegment.rightControlId] = leftControl2;
            controls[rightHandleSegment.leftControlId] = rightControl1;
            controls[rightHandleSegment.rightControlId] = rightControl2;
        }
        if (valueY != float.NegativeInfinity)
        {
            oldHandle.y = valueY;
            foreach (SegmentBinding binding in segmentBindings)
            {
                if (binding.leftHandleId == handleID)
                {
                    binding.curve.a = 1f - valueY;
                }
                if (binding.rightHandleId == handleID)
                {
                    binding.curve.d = 1f - valueY;
                }
            }
        }

        handles[handleID] = oldHandle;
        return oldHandle;
    }

    private Rect SetControlValue(int controlID, float value)
    {
        Rect oldControl = controls[controlID];
        oldControl.y = value;

        foreach (SegmentBinding binding in segmentBindings)
        {
            if (binding.leftControlId == controlID)
            {
                binding.curve.b = 1f - value;
            }
            if (binding.rightControlId == controlID)
            {
                binding.curve.c = 1f - value;
            }
        }

        controls[controlID] = oldControl;
        return oldControl;
    }

    private void PlaceWindows(List<Rect> list, bool isHandle)
    {
        GUI.color = (isHandle) ? Color.white : new Color(53f / 255f, 134f / 255f, 1f);
        for (int i = 0; i < list.Count; i++)
        {
            Rect windowPosition = list[i];
            windowPosition.x *= canvasSize.x;   //window x value is normalized, so scale it by canvas size
            windowPosition.x = Mathf.Clamp(windowPosition.x, 0, canvasSize.x - handleSizeX); //Make sure window doesn't draw off screen
            if (i > 0 && i < list.Count - 1)
                windowPosition.x -= handleSizeX / 2f; //Offset window so the center draws over the point, not top right
            //repeat for y values
            windowPosition.y *= canvasSize.y;
            windowPosition.y += curveYOffset;
            windowPosition.y -= handleSizeY / 2f;

            if (isHandle)
            {
                GUI.Window(i, windowPosition, DrawHandle, new GUIContent(_white));
            }
            else
            {
                GUI.Window(i, windowPosition, DrawControl, new GUIContent(_blue));
            }
        }
        GUI.color = Color.white;
    }

    //Function to manage a handle being dragged. dragHandle is expected to not be null
    private void DragHandle()
    {
        if (Event.current.type == EventType.MouseUp)
        {
            dragHandle = null;
            Event.current.Use();
        }
        else if (Event.current.type == EventType.MouseDrag)
        {
            MoveWindow();
            Event.current.Use();
            Repaint();
        }
    }

    private void DrawWindow(int id, bool isHandle)
    {
        Rect rect;
        if (isHandle)
        {
            rect = handles[id];
        }
        else
        {
            rect = controls[id];
        }

        //Detect click inside this window and set the window as being dragged if the event is inside
        if (dragHandle == null && Event.current.type == EventType.MouseDown
            && Event.current.mousePosition.x >= 0 && Event.current.mousePosition.x < rect.width
            && Event.current.mousePosition.y >= 0 && Event.current.mousePosition.y < rect.height)
        {
            dragHandle = new DragInfo();
            dragHandle.id = id;
            if (isHandle)
            {
                dragHandle.list = handles;
            }
            else
            {
                dragHandle.list = controls;
            }
            dragHandle.isHandle = isHandle;

            MoveWindow();
            Repaint();
            Event.current.Use();
        }
    }

    private void MoveWindow()
    {
        if (dragHandle == null)
            return;

        //Window being moved
        Rect window = dragHandle.list[dragHandle.id];

        if (dragHandle.isHandle)
        {

            //Find left and right bound segment for this handle
            SegmentBinding rightHandleSegment = null;
            SegmentBinding leftHandleSegment = null;

            foreach (SegmentBinding binding in segmentBindings)
            {
                if (binding.leftHandleId == dragHandle.id)
                {
                    leftHandleSegment = binding;
                }
                if (binding.rightHandleId == dragHandle.id)
                {
                    rightHandleSegment = binding;
                }
            }

            if (leftHandleSegment == null || rightHandleSegment == null || shiftHeld)
            {
                SetHandleValue(dragHandle.id, window.position.x, window.position.y + Event.current.delta.y / canvasSize.y);
            }
            else
            {
                float newXPosition = window.position.x + Event.current.delta.x / canvasSize.x;
                float newYPosition = window.position.y + Event.current.delta.y / canvasSize.y;
                SetHandleValue(dragHandle.id, newXPosition, newYPosition);
            }
        }
        else
        {
            //Control's position is locked to the y axis and position is directly related to curve value
            SetControlValue(dragHandle.id, window.position.y + Event.current.delta.y / canvasSize.y);
        }
    }

    void DrawNodeCurve()
    {
        GUILayout.BeginVertical();
        var rect = new Rect(0, 0, canvasSize.x, canvasSize.y);
        Handles.color = Color.black;

        float yMin = 0;
        float yMax = 1;
        float step = 1 / rect.width;

        Vector3 prevPos = new Vector3(0, curve.GetValue(0), 0);
        for (float t = step; t < 1; t += step)
        {
            foreach (CurveRegionColor colorRegion in curveColors)
            {
                if (t >= colorRegion.range.x && t < colorRegion.range.y)
                {
                    Handles.color = colorRegion.color;
                }
                else
                {
                    Handles.color = Color.black;
                }
            }
            Vector3 pos = new Vector3(t, curve.GetValue(t), 0);
            UnityEditor.Handles.DrawLine(
                new Vector3(rect.xMin + prevPos.x * rect.width, curveYOffset + (rect.yMax - ((prevPos.y - yMin) / (yMax - yMin)) * rect.height), 0),
                new Vector3(rect.xMin + pos.x * rect.width, curveYOffset + (rect.yMax - ((pos.y - yMin) / (yMax - yMin)) * rect.height), 0)
            );

            prevPos = pos;
        }
        GUILayout.EndVertical();
    }
}
