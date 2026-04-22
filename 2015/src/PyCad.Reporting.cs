using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string ExportEntityInfo(ObjectId entityId, string filePath)
        {
            Hashtable info = GetEntityInfo(entityId);
            string fullPath = Path.GetFullPath(filePath);
            EnsureParentDirectory(fullPath);

            StringBuilder sb = new StringBuilder();
            foreach (DictionaryEntry item in info)
            {
                sb.AppendLine(item.Key + "=" + item.Value);
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        public string ExportEntitiesInfo(IList entityIds, string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            EnsureParentDirectory(fullPath);

            StringBuilder sb = new StringBuilder();
            foreach (object raw in entityIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                Hashtable info = GetEntityInfo((ObjectId)raw);
                foreach (DictionaryEntry item in info)
                {
                    sb.Append(item.Key).Append("=").Append(item.Value).Append(";");
                }
                sb.AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        public string ExportPolylineVertices(ObjectId entityId, string filePath)
        {
            ArrayList verts = GetPolylineVertices(entityId);
            string fullPath = Path.GetFullPath(filePath);
            EnsureParentDirectory(fullPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("index,x,y,z");
            foreach (object raw in verts)
            {
                Hashtable item = raw as Hashtable;
                if (item == null)
                {
                    continue;
                }

                sb.Append(item["index"]).Append(",");
                sb.Append(ToCsvValue(item["x"])).Append(",");
                sb.Append(ToCsvValue(item["y"])).Append(",");
                sb.Append(ToCsvValue(item["z"])).AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        public string ExportSelectionToCsv(IList entityIds, string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            EnsureParentDirectory(fullPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("id,handle,type,layer,color_index,is_erased,min_x,min_y,min_z,max_x,max_y,max_z");

            foreach (object raw in entityIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                Hashtable info = GetEntityInfo((ObjectId)raw);
                sb.Append(ToCsvValue(GetHashValue(info, "id"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "handle"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "type"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "layer"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "color_index"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "is_erased"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "min_x"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "min_y"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "min_z"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "max_x"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "max_y"))).Append(",");
                sb.Append(ToCsvValue(GetHashValue(info, "max_z"))).AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        public Hashtable BuildSelectionSummary(IList entityIds)
        {
            Hashtable summary = new Hashtable();
            Hashtable byType = new Hashtable();
            Hashtable byLayer = new Hashtable();
            int total = 0;

            foreach (object raw in entityIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                Hashtable info = GetEntityInfo((ObjectId)raw);
                string type = Convert.ToString(GetHashValue(info, "type"));
                string layer = Convert.ToString(GetHashValue(info, "layer"));

                total++;
                IncrementHashCounter(byType, type);
                IncrementHashCounter(byLayer, layer);
            }

            summary["total"] = total;
            summary["by_type"] = byType;
            summary["by_layer"] = byLayer;
            return summary;
        }

        private static void EnsureParentDirectory(string fullPath)
        {
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static object GetHashValue(Hashtable ht, string key)
        {
            return ht.ContainsKey(key) ? ht[key] : string.Empty;
        }

        private static void IncrementHashCounter(Hashtable ht, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "<empty>";
            }

            if (ht.ContainsKey(key))
            {
                ht[key] = Convert.ToInt32(ht[key], CultureInfo.InvariantCulture) + 1;
            }
            else
            {
                ht[key] = 1;
            }
        }

        private static string ToCsvValue(object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            if (text.Contains("\""))
            {
                text = text.Replace("\"", "\"\"");
            }
            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
            {
                text = "\"" + text + "\"";
            }
            return text;
        }
    }
}
