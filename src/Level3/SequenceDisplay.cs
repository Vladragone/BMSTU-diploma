using UnityEngine;

public class SequenceDisplay : MonoBehaviour
{
    [Header("Display Cubes")]
    public Renderer slot1;
    public Renderer slot2;
    public Renderer slot3;

    [Header("Materials")]
    public Material grayMaterial;
    public Material redMaterial;
    public Material blueMaterial;
    public Material greenMaterial;

    private Renderer[] slots;

    private void Awake()
    {
        slots = new Renderer[] { slot1, slot2, slot3 };
    }

    public void ResetDisplay()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SetSlotGray(i);
        }
    }

    public void SetSlotColor(int index, ColorType colorType)
    {
        if (index < 0 || index >= slots.Length)
            return;

        slots[index].material = GetMaterial(colorType);
    }

    private void SetSlotGray(int index)
    {
        if (slots[index] != null)
        {
            slots[index].material = grayMaterial;
        }
    }

    private Material GetMaterial(ColorType colorType)
    {
        switch (colorType)
        {
            case ColorType.Red:
                return redMaterial;

            case ColorType.Blue:
                return blueMaterial;

            case ColorType.Green:
                return greenMaterial;
        }

        return grayMaterial;
    }
}