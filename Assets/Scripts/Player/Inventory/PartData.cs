using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SQLite4Unity3d;

public class PartData
{
	// SQLite Net does only support single-column primary keys...
	[PrimaryKey]
	public string key {
		get {
			return part + "-" + status;
		}
	}
	public int part;
	public int status = 100;
	public int quantity;

	private LowData getData ()
	{
		return new LowData {
			key = key,
			part = part,
			status = status,
			quantity = quantity,
		};
	}

	public void save () {
		if (quantity < 1) {
			delete();
		} else {
			Service.db.Update (this.getData());
		}
	}
	
	public bool create () {
		if (Service.db.Insert(this.getData()) > 0) {
			return true;
		}
		return false;
	}
	
	public void delete () {
		Service.db.Delete<LowData> (this.key);
	}

	public static string getKey (CarEngine.Part part, int status) {
		return (int)part + "-" + status;
	}

	public static PartData getOne (System.Linq.Expressions.Expression<System.Func<LowData, bool>> predExpr) {
		var result = Service.getOne<LowData>(predExpr);

		if (result == null) {
			return null;
		}

		return new PartData {
			part = result.part,
			status = result.status,
			quantity = result.quantity,
		};
	}

	public override string ToString ()
	{
		return string.Format ("[PartData: Key={0}, Part={1},  Status={2}, Quantity={3}]", key, part, status, quantity);
	}
}
/*
 * SQLite is quite bad integrated in Unity, it requires a data only table to work properlly.. Next time i should use iBoxDB.net again
 **/
public class LowData
{
	[PrimaryKey]
	public string key { get; set; }
	public int part { get; set; }
	public int status { get; set; }
	public int quantity { get; set; }
}