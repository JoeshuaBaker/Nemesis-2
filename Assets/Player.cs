using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;

public class Player : MonoBehaviour
{
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer limbRenderer;
    public string spriteSheetDirectory;
    public string bodySpriteName;
    public string[] limbSpriteName;
    public float limbTransitionTime = 0.5f;
    public Transform hpBar;
    public ProjectileEmitterAdvanced[] guns;
    public int maxHp = 100;
    public bool isSplit = false;
    public float splitAmount = 0.35f;
    public int currentHp = 100;
    public float fastVel = 1.5f;
    public float slowVel = 0.5f;
    private float slowFastRatio = 0.5f / 1.5f;
    public float leftRightDrift = 0f;
    private Vector2 currentVel = new Vector2(0f, 0f);
    public Sprite[] bodySprites;
    public Sprite[][] limbSprites;
    private Vector2[] vectors;
    private Vector2 mouseDirection;
    public AK.Wwise.Event clapEvent;

    private void Start() {
        currentHp = maxHp;
        slowFastRatio = slowVel / fastVel;

        if(bodyRenderer == null || limbRenderer == null)
        {
            Debug.LogError("Setup spriterenderers on Player " + this.name);
        }

        string bodyPath = spriteSheetDirectory + bodySpriteName;
        bodySprites = Resources.LoadAll<Sprite>(bodyPath);

        limbSprites = new Sprite[limbSpriteName.Length][];
        for(int i = 0; i < limbSprites.Length; i++)
        {
            limbSprites[i] = Resources.LoadAll<Sprite>(spriteSheetDirectory + limbSpriteName[i]);
        }

        vectors = new Vector2[bodySprites.Length];
        float degrees = 360.0f/(float)vectors.Length;
        float currentDegrees = 0;
        for(int i = 0; i < vectors.Length; i++)
        {
            Vector3 v3 = (Quaternion.Euler(0, 0, currentDegrees) * Vector3.right);
            vectors[i].x = v3.x;
            vectors[i].y = v3.y;
            currentDegrees += degrees;
        }
    }
    
    private void Update() {
        Move();
        MouseAim();
        Shoot();
    }

    public void updateHp(int hpChange) {
        currentHp += hpChange;
        if(currentHp > maxHp)
        {
            currentHp = maxHp;
        }
        else if(currentHp < 0)
        {
            currentHp = maxHp;
        }
        hpBar.localScale = new Vector3((float)currentHp/(float)maxHp, 1, 1);
    }

    private void Move() {
        Vector2 holdDirection = new Vector2(0,0);
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            holdDirection.y += 1;
        }
        if(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            holdDirection.y -= 1;
        }
        if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            holdDirection.x -= 1;
        }
        if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            holdDirection.x += 1;
        }

        if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentVel = holdDirection * slowVel;
            leftRightDrift += holdDirection.x * slowFastRatio * Time.deltaTime;
        }
        else
        {
            currentVel = holdDirection * fastVel;
            leftRightDrift += holdDirection.x * Time.deltaTime;
        }

        if(holdDirection.x == 0 && leftRightDrift != 0f)
        {
            if(Math.Abs(leftRightDrift) < Time.deltaTime)
            {
                leftRightDrift = 0f;
            }
            else if(leftRightDrift > 0f)
            {
                leftRightDrift -= Time.deltaTime;
            }
            else
            {
                leftRightDrift += Time.deltaTime;
            }
        }

        this.transform.position = new Vector3(
            this.transform.position.x + currentVel.x*Time.deltaTime,
            this.transform.position.y + currentVel.y*Time.deltaTime,
            this.transform.position.z
        );
    }

    private void MouseAim()
    {
        int dir = 1;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z;
        Vector3 worldMouse = Camera.main.ScreenToWorldPoint(mousePos);
        Vector3 dirToMouse = worldMouse - this.transform.position;
        Vector2 xy = new Vector2(dirToMouse.x, dirToMouse.y);
        xy = xy.normalized;
        mouseDirection = xy;

        foreach (var gun in guns)
        {
            if(isSplit)
            {
                Vector2 offset = new Vector2(xy.y*dir, -xy.x*dir);
                offset = offset.normalized;
                offset = offset*splitAmount;
                xy = xy + offset;
                xy = xy.normalized;
            }

            gun.transform.localPosition = xy/3;
            gun.props.Direction = xy;
            if(isSplit)
                dir *= -1;
        }

        DrawSprites();

    }

    private void DrawSprites()
    {
        float lowestDist = float.MaxValue;
        int lowestIndex = -1;
        for (int i = 0; i < vectors.Length; i++)
        {
            float dx = vectors[i].x - mouseDirection.x;
            float dy = vectors[i].y - mouseDirection.y;
            float dist = Mathf.Abs(dx) + Mathf.Abs(dy);
            if (dist < lowestDist)
            {
                lowestDist = dist;
                lowestIndex = i;
            }
        }

        //body sprite selection
        bodyRenderer.sprite = bodySprites[lowestIndex];

        //limb sprite direction
        float ceil = limbTransitionTime * (limbSprites.Length / 2) + 0.01f;
        float floor = -1 * ceil;
        leftRightDrift = Mathf.Clamp(leftRightDrift, floor, ceil);
        int regionsSearched = 0;
        float region = limbTransitionTime;
        while(Math.Abs(leftRightDrift) > Math.Abs(region))
        {
            region += limbTransitionTime;
            regionsSearched++;
        }

        int index = limbSprites.Length / 2;
        if(leftRightDrift > 0)
        {
            index += regionsSearched;
        }
        else
        {
            index -= regionsSearched;
        }

        if(index < 0 || index > limbSprites.Length)
        {
            Debug.LogError("Could not find limb lookup for leftRightDrift value of " + leftRightDrift);
            return;
        }

        Sprite[] limbDriftSprites = limbSprites[index];
        limbRenderer.sprite = limbDriftSprites[lowestIndex];
    }

    private void Shoot()
    {
        foreach(var gun in guns) 
        {
            if(Input.GetKey(KeyCode.Space))
            {
                if(gun.props.AutoFire == false)
                {
                    clapEvent.Post(gameObject);
                }
                gun.props.AutoFire = true;
            }
            else
            {
                gun.props.AutoFire = false;
            }
        }
    }

}
