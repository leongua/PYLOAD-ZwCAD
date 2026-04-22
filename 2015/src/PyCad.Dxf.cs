using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string GetDxfName(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                if (dbo == null)
                {
                    throw new ArgumentException("DBObject non trovato");
                }
                return dbo.GetRXClass().DxfName;
            }
        }

        public string[] ListXDataApps(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                if (dbo == null)
                {
                    throw new ArgumentException("DBObject non trovato");
                }

                ResultBuffer rb = dbo.XData;
                List<string> apps = new List<string>();
                if (rb == null)
                {
                    return apps.ToArray();
                }

                string currentApp = null;
                int currentCount = 0;
                foreach (TypedValue tv in rb)
                {
                    if (tv.TypeCode == 1001)
                    {
                        if (!string.IsNullOrWhiteSpace(currentApp) && currentCount > 0 && !apps.Contains(currentApp))
                        {
                            apps.Add(currentApp);
                        }
                        currentApp = Convert.ToString(tv.Value);
                        currentCount = 0;
                    }
                    else if (!string.IsNullOrWhiteSpace(currentApp))
                    {
                        currentCount++;
                    }
                }

                if (!string.IsNullOrWhiteSpace(currentApp) && currentCount > 0 && !apps.Contains(currentApp))
                {
                    apps.Add(currentApp);
                }

                return apps.ToArray();
            }
        }

        public Hashtable GetXData(ObjectId entityId, string appName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                if (dbo == null)
                {
                    throw new ArgumentException("DBObject non trovato");
                }

                ResultBuffer rb = dbo.GetXDataForApplication(appName);
                Hashtable result = new Hashtable();
                ArrayList values = new ArrayList();
                result["app"] = appName;
                result["values"] = values;

                if (rb == null)
                {
                    result["count"] = 0;
                    return result;
                }

                foreach (TypedValue tv in rb)
                {
                    if (tv.TypeCode == 1001)
                    {
                        continue;
                    }

                    Hashtable item = new Hashtable();
                    item["type_code"] = tv.TypeCode;
                    item["value"] = Convert.ToString(tv.Value);
                    values.Add(item);
                }

                result["count"] = values.Count;
                return result;
            }
        }

        public void SetXData(ObjectId entityId, string appName, IList typedValues)
        {
            EnsureRegApp(appName);

            List<TypedValue> values = new List<TypedValue>();
            values.Add(new TypedValue(1001, appName));

            foreach (object raw in typedValues)
            {
                Hashtable item = raw as Hashtable;
                if (item == null)
                {
                    continue;
                }

                short typeCode = Convert.ToInt16(item["type_code"]);
                object value = item["value"];
                values.Add(new TypedValue(typeCode, value));
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForWrite);
                if (dbo == null)
                {
                    throw new ArgumentException("DBObject non trovato");
                }

                using (ResultBuffer rb = new ResultBuffer(values.ToArray()))
                {
                    dbo.XData = rb;
                }

                tr.Commit();
            }
        }

        public void ClearXData(ObjectId entityId, string appName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForWrite);
                if (dbo == null)
                {
                    throw new ArgumentException("DBObject non trovato");
                }

                ResultBuffer existing = dbo.XData;
                if (existing == null)
                {
                    tr.Commit();
                    return;
                }

                List<TypedValue> kept = new List<TypedValue>();
                bool skipping = false;
                foreach (TypedValue tv in existing)
                {
                    if (tv.TypeCode == 1001)
                    {
                        string app = Convert.ToString(tv.Value);
                        skipping = string.Equals(app, appName, StringComparison.OrdinalIgnoreCase);
                        if (!skipping)
                        {
                            kept.Add(tv);
                        }
                    }
                    else if (!skipping)
                    {
                        kept.Add(tv);
                    }
                }

                if (kept.Count == 0)
                {
                    using (ResultBuffer rb = new ResultBuffer(new TypedValue(1001, appName)))
                    {
                        dbo.XData = rb;
                    }
                }
                else
                {
                    using (ResultBuffer rb = new ResultBuffer(kept.ToArray()))
                    {
                        dbo.XData = rb;
                    }
                }

                tr.Commit();
            }
        }

        public Hashtable GetEntityRawSummary(ObjectId entityId)
        {
            Hashtable info = GetEntityCommonInfo(entityId);
            info["dxf_name"] = GetDxfName(entityId);
            info["xdata_apps"] = ListXDataApps(entityId);
            return info;
        }

        private void EnsureRegApp(string appName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                RegAppTable table = (RegAppTable)tr.GetObject(_db.RegAppTableId, OpenMode.ForRead);
                if (!table.Has(appName))
                {
                    table.UpgradeOpen();
                    RegAppTableRecord rec = new RegAppTableRecord();
                    rec.Name = appName;
                    table.Add(rec);
                    tr.AddNewlyCreatedDBObject(rec, true);
                }
                tr.Commit();
            }
        }
    }
}
