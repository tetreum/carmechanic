using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Inventory : MonoBehaviour {

	public GameObject itemPrefab;

	// Fill inventory slots
	void OnEnable ()
	{
		var list = Service.db.Table<LowData>().Where(x => x.quantity > 0);
		Vector3 pos = itemPrefab.transform.position;
		InventoryItemPanel panelData;

		foreach (var item in list)
		{
			GameObject obj = (GameObject)Object.Instantiate(itemPrefab, pos, Quaternion.identity);

			obj.transform.SetParent(this.transform, false);
			obj.transform.position = pos;
			obj.transform.tag = "TemporalPanel";
	
			panelData = obj.GetComponent<InventoryItemPanel>();
			panelData.name.text = Service.partsList[(CarEngine.Part)item.part].name;
			panelData.status.text = item.status + "%";
			panelData.quantity.text = "x" + item.quantity;

			obj.SetActive(true);
			pos.x += itemPrefab.GetComponent<RectTransform>().rect.width;
		}
	}

	// Destroy slots as they may get outdated
	void OnDisable ()
	{
		List<GameObject> children = new List<GameObject>();

		foreach (Transform child in transform) {
			if (child.tag == "TemporalPanel") {
				children.Add(child.gameObject);
			}
		} 
		children.ForEach(child => Destroy(child));
	}

	public static bool add (CarEngine.Part type, int status, int amount)
	{
		bool success = false;
		string key = PartData.getKey(type, status);

		PartData part = PartData.getOne(x => x.key == key);

		if (part != null) {
			part.quantity += amount;
			part.save();
			success = true;
		} else {
			PartData item = new PartData {
				part = (int)type,
				quantity = amount,
				status = status
			};
			success = item.create ();
		}
		return success;
	}

	public static bool del (CarEngine.Part type, int status, int amount)
	{
		bool success = false;
		string key = PartData.getKey(type, status);

		PartData part = PartData.getOne(x => x.key == key);
		
		if (part != null) {
			part.quantity -= amount;
			part.save();
			success = true;
		}

		return success;
	}
}
