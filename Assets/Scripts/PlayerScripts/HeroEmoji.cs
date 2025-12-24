using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroEmoji : MonoBehaviour
{
    public SkinnedMeshRenderer faceRenderer;
    public List<Material> faces;

    public void SetDefaultFace()
    {
        SetFace(0);
    }

    public void SetFaceRandom()
    {
        SetFace(Random.Range(0, faces.Count));
    }

    public void SetFaceChance(float chance)
    {
        if (Random.Range(0, 100) < chance)
        {
            SetFaceRandom();
        }
    }

    public void SetFaceChance(int index, float chance)
    {
        if (Random.Range(0, 100) < chance)
        {
            SetFace(index);
        }
    }

    public void SetFace(int index)
    {
        try
        {
            faceRenderer.material = faces[index];
        }
        catch 
        {
        }
    }

}
