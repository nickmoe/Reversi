using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool isActivated;
    public bool inPlace;
    private bool rotate;

    private Transform target;
    float xBoard;
    float zBoard;
    string nameP;

    // Start is called before the first frame update
    void Start()
    {
        isActivated = false;
        inPlace = false;
        rotate = false;

        nameP = gameObject.transform.name;
        xBoard = (float) char.GetNumericValue(name[6]) * -1.0f;
        zBoard = (float) char.GetNumericValue(name[8]) + 0.0f;

        target = new GameObject().transform;
        target.transform.position = new Vector3(xBoard, 1.0f, zBoard);
    }

    // Update is called once per frame
    void Update()
    {
        if (rotate)
        {
            float step = 2.0f * Time.deltaTime;
        }

        if (!inPlace && isActivated)
        {
            float step = 2.0f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);

            if (transform.position == target.position) inPlace = true;
        }
        
    }

    public void activatePiece(bool isBlack)
    {
        if (isActivated) return;

        isActivated = true;

        if (!isBlack)
        {
            if(gameObject.tag == "Black")
            {
                flipPiece();
            }
        }
        else{
            if (gameObject.tag == "White")
            {
                flipPiece();
            }
        }

        
    }

    public void flipPiece()
    {
        transform.Rotate(180, 0, 0);
    }
}
