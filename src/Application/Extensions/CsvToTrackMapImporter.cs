using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Shared.Enums;
using Google.Protobuf.WellKnownTypes;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;

namespace NextAdmin.Application.Extensions
{
    public static class CsvToTrackMapImporter
    {
        public static TrackMap ImportFromCsv(string trackCsv, string nodeCsv,  string vehicleCsv)
        {
            var nodeIdMap = new Dictionary<int, ObjectId>();
            var nodeObjMap = new Dictionary<int, TrackNode>();
            var trackNodeList = new List<TrackNode>();
            var canParkNodes = new List<TrackNode>();

            var mapId = ObjectId.GenerateNewId();

            // 1. 读取trackNode.csv
            var nodes = File.ReadAllLines(nodeCsv).Skip(1); // 跳过表头

            // 2. 读取track.csv
            var trackList = new List<Track>();
            var trackLines = File.ReadAllLines(trackCsv).Skip(1);

            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node)) continue;
                var cols = SplitCsv(node);
                int id = int.Parse(cols[0]);
                double x = double.Parse(cols[1], CultureInfo.InvariantCulture);
                double y = double.Parse(cols[2], CultureInfo.InvariantCulture);
                int type = int.TryParse(cols[3], out var t) ? t : 1;
                string name = cols[4];
                NodeType nodeType = NodeType.Normal;

                if (name.Contains("起始点"))
                {
                    nodeType = NodeType.Parking;
                }
                else if (name.Contains("转台"))
                {
                    nodeType = NodeType.TurningPoint;
                }

                var trackNode = new TrackNode
                (
                    name,
                    nodeType,
                    mapId, 
                    x,
                    1080 - y
                );

                nodeIdMap[id] = trackNode.Id;
                nodeObjMap[id] = trackNode;
                trackNodeList.Add(trackNode);
                //if (nodeType == NodeType.Parking) canParkNodes.Add(trackNode);
            }

            foreach (var line in trackLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var lineCols = SplitCsv(line);
                int lineId = int.Parse(lineCols[0]);
                int fromId = int.Parse(lineCols[1]);
                int toId = int.Parse(lineCols[2]);
                string lineName = lineCols[3];

                double.TryParse(lineCols[11], out var length);

                if (length == 0)
                {
                    var startNode = trackNodeList.FirstOrDefault(n => n.Id == nodeObjMap[fromId].Id);
                    var endNode = trackNodeList.FirstOrDefault(n => n.Id == nodeObjMap[toId].Id);
                    if (startNode == null || endNode == null)
                    {
                        throw new Exception($"{lineName}没有连接节点");
                    }

                    var dx = startNode.X - endNode.X;
                    var dy = startNode.Y - endNode.Y;
                    length = Math.Sqrt(dx * dx + dy * dy);
                }



                var track = Track.Create(
                    mapId, // 稍后赋值
                    nodeIdMap[fromId],
                    nodeIdMap[toId],
                    length,
                    3.0, // 默认宽度
                    TrackType.Main, // 默认类型
                    true,
                    lineName
                );
                track.NodeIds.Add(nodeIdMap[fromId]);
                track.NodeIds.Add(nodeIdMap[toId]);
                trackList.Add(track);
            }


            // 3. 读取vehicle.csv
            var vehicleList = new List<Vehicle>();
            var vehicleLines = File.ReadAllLines(vehicleCsv).Skip(1);
            int parkIdx = 0;
            canParkNodes = trackNodeList.Where(n => n.Type == NodeType.Parking).ToList();
            foreach (var line in vehicleLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsv(line);
                string name = cols[1];
                int type = int.TryParse(cols[3], out var t) ? t : 1;
                // 分配可停车节点
                var parkNode = canParkNodes[parkIdx];
                parkIdx++;
                var vehicle = new Vehicle
                (    name,
                    (float)parkNode.X,
                    (float)parkNode.Y,
                    mapId, // 稍后赋值
                    parkNode.Id
                );
                parkNode.VehicleId= vehicle.Id;
                parkNode.Status = NodeStatus.Occupied;
                vehicleList.Add(vehicle);
            }

            // 4. 组装TrackMap
            //foreach (var n in trackNodeList) n.MapId = mapId;
            //foreach (var t in trackList) t.MapId = mapId;
            //foreach (var v in vehicleList) v.MapId = mapId;

            // 轨道和节点关联
            foreach (var node in trackNodeList)
            {
                node.ConnectedTacks = trackList
                    .Where(t => t.StartNodeId == node.Id || t.EndNodeId == node.Id)
                    .ToList();
            }

            var trackMap = new TrackMap(mapId, "轨道地图", "v1.0", "由CSV导入")
            {
                TrackNodes = trackNodeList,
                Tracks = trackList,
                Vehicles = vehicleList
            };

            return trackMap;
        }

        public static void ExportToJson(TrackMap map, string jsonPath)
        {
            var json = map.ToJson(new IO.JsonWriterSettings { Indent = true });
            File.WriteAllText(jsonPath, json);
        }

        // 简单CSV分割，支持逗号分隔和引号包裹
        private static string[] SplitCsv(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var value = "";
            foreach (var c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == ',' && !inQuotes) { result.Add(value); value = ""; continue; }
                value += c;
            }
            result.Add(value);
            return result.ToArray();
        }
    }
} 
