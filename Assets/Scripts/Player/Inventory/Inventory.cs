using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public GameObject itemPrefab;

    // Fill inventory slots
    private void OnEnable()
    {
        var list = Service.db.Table<LowData>().Where(x => x.quantity > 0);
        var pos = itemPrefab.transform.position;
        InventoryItemPanel panelData;

        foreach (var item in list)
        {
            var obj = Instantiate(itemPrefab, pos, Quaternion.identity);

            obj.transform.SetParent(transform, false);
            obj.transform.position = pos;
            obj.transform.tag = "TemporalPanel";

            panelData = obj.GetComponent<InventoryItemPanel>();
            panelData.name.text = Service.partsList[(CarEngine.Part) item.part].Name;
            panelData.status.text = item.status + "%";
            panelData.quantity.text = "x" + item.quantity;

            obj.SetActive(true);
            pos.x += itemPrefab.GetComponent<RectTransform>().rect.width;
        }
    }

    // Destroy slots as they may get outdated
    private void OnDisable()
    {
        var children = new List<GameObject>();

        foreach (Transform child in transform)
            if (child.tag == "TemporalPanel")
                children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));
    }

    public static bool add(CarEngine.Part type, int status, int amount)
    {
        var success = false;
        var key = PartData.getKey(type, status);

        var part = PartData.getOne(x => x.key == key);

        if (part != null)
        {
            part.quantity += amount;
            part.save();
            success = true;
        }
        else
        {
            var item = new PartData
            {
                part = (int) type,
                quantity = amount,
                status = status
            };
            success = item.create();
        }

        return success;
    }

    public static bool del(CarEngine.Part type, int status, int amount)
    {
        var success = false;
        var key = PartData.getKey(type, status);

        var part = PartData.getOne(x => x.key == key);

        if (part != null)
        {
            part.quantity -= amount;
            part.save();
            success = true;
        }

        return success;
    }
}