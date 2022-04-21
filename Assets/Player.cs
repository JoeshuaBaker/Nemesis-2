using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell;

public class Player : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public string spriteSheetDirectory;
    public Transform hpBar;
    public ProjectileEmitterAdvanced[] guns;
    public int maxHp = 100;
    public bool isSplit = false;
    public float splitAmount = 0.35f;
    public int currentHp = 100;
    public float fastVel = 1.5f;
    public float slowVel = 0.5f;
    private Vector2 currentVel = new Vector2(0f, 0f);
    private Sprite[] sprites;
    private Vector2[] vectors;
    private Vector2 mouseDirection;

    private void Start() {
        currentHp = maxHp;
        spriteRenderer = GetComponent<SpriteRenderer>();
        sprites = Resources.LoadAll<Sprite>(spriteSheetDirectory);
        vectors = new Vector2[sprites.Length];
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
        }
        else
        {
            currentVel = holdDirection * fastVel;
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
        foreach(var gun in guns)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = -Camera.main.transform.position.z;
            Vector3 worldMouse = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3 dirToMouse = worldMouse - this.transform.position;
            Vector2 xy = new Vector2(dirToMouse.x, dirToMouse.y);
            xy = xy.normalized;

            this.mouseDirection = xy;
            float lowestDist = float.MaxValue;
            int lowestIndex = -1;
            for(int i = 0; i < vectors.Length; i++)
            {
                float dx = vectors[i].x - xy.x;
                float dy = vectors[i].y - xy.y;
                float dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                if(dist < lowestDist)
                {
                    lowestDist = dist;
                    lowestIndex = i;
                }
            }

            if(isSplit)
            {
                Vector2 offset = new Vector2(xy.y*dir, -xy.x*dir);
                offset = offset.normalized;
                offset = offset*splitAmount;
                xy = xy + offset;
                xy = xy.normalized;
            }

            spriteRenderer.sprite = sprites[lowestIndex];
            gun.transform.localPosition = xy/3;
            gun.props.Direction = xy;
            if(isSplit)
                dir *= -1;
        }
        
    }

    private void Shoot()
    {
        foreach(var gun in guns) 
        {
            if(Input.GetKey(KeyCode.Space))
            {
                gun.props.AutoFire = true;
            }
            else
            {
                gun.props.AutoFire = false;
            }
        }
    }

}
