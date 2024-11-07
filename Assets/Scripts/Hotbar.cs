using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HotBar : MonoBehaviour
{
    public RectTransform highlight;
    public Player player;
    public UIItemSlot[] slots;
    public int slotIndex = 0;

    private void Start()
    {
        byte index = 1;

        foreach (UIItemSlot s in slots)
        {
            ItemStack stack = new ItemStack(index++, 14);

            ItemSlot slot = new ItemSlot(s, stack);
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {

            if (scroll > 0)
                slotIndex--;
            else
                slotIndex++;

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = slots.Length - 1;

            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
    }
}
