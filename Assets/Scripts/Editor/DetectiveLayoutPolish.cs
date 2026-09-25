using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using static Detective.EditorTools.DetectiveCityBuilder;

namespace Detective.EditorTools
{
    // Incremental authored dressing. Existing scene objects are preserved.
    public static class DetectiveLayoutPolish
    {
        static readonly Color Wood = new(0.29f, 0.23f, 0.18f);
        static readonly Color Metal = new(0.23f, 0.27f, 0.28f);
        static readonly Color Leaf = new(0.16f, 0.26f, 0.20f);
        static readonly Color Stone = new(0.30f, 0.29f, 0.26f);
        static Transform group;
        static Vector3[] route;
        static bool outdoor;
        static readonly List<string> skipped = new();

        public static string Apply(Transform root, string id)
        {
            if (root.Find("LayoutDetails") != null) return id + ": already applied";
            ResetMaterialCaches();
            if (id == "I2_Coffee") MoveCoffee(root);
            if (id == "I1_Bar")
            {
                var bartender = root.Find("Bartender").GetComponentInChildren<TestInteractable>(true);
                var so = new SerializedObject(bartender);
                so.FindProperty("interactionRange").floatValue = 2.4f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            group = new GameObject("LayoutDetails").transform;
            group.SetParent(root, false);
            skipped.Clear();
            outdoor = id.StartsWith("D") || id == "I3_Rooftop";
            route = id switch
            {
                "D0_Alley" => Points(0,-9, 0,-2, 0,6, 0,12),
                "D1_RedLight" => Points(-20,0, -16,0, -12,3, -5,0, 7,0, 20,0),
                "I1_Bar" => Points(0,-3.3f, -0.5f,-1, -4.8f,-1, -4.8f,2.8f),
                "D2_Financial" => Points(-20,0, -8,0, 4,-1, 10,-4, 20,0),
                "I2_Apartment" => Points(0,-4, 0,-1, 0,3, 3.5f,3.8f, 6.5f,3.8f),
                "I2_Coffee" => Points(3.5f,-3.6f, 0,-2, -2,-0.6f, -2,1.4f),
                "D3_Industrial" => Points(-21,0, -14,0, -6,0, 2,-2, 12,-3.5f, 22,-5),
                "I3_Warehouse" => Points(0,-5, 0,-2, -2.5f,-1, -2.5f,2.8f),
                "I3_Basement" => Points(5.5f,0, 0,0, -3,1, 0,2, 3,1),
                "I3_Rooftop" => Points(4.5f,-5.8f, 2,-3, -2,-3, -6,-4),
                "D4_Residential" => Points(-18,0, -10,0, -2,0, 6,3, 14,3, 14,7),
                "I4_YourHome" => Points(0,-3.7f, 0,-1, -3,0.6f, -3,2.8f, 2,2.8f),
                _ => Points(0,-3.7f, 0,-0.3f, 2,1, 3.5f,2.5f)
            };
            // Subtle paving/runners establish a continuous main route; no arrows or clue labels.
            for (int i = 1; i < route.Length; i++)
            {
                var delta = route[i] - route[i-1];
                var strip = CreateCube(group, "Walkway_" + i, (route[i]+route[i-1])*0.5f + Vector3.up*0.008f,
                    new Vector3(outdoor ? 2.4f : 1.25f, 0.012f, delta.magnitude), outdoor ? new Color(.23f,.23f,.22f) : new Color(.25f,.22f,.20f));
                strip.transform.rotation = Quaternion.LookRotation(delta);
                Object.DestroyImmediate(strip.GetComponent<Collider>());
                strip.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            }
            Physics.SyncTransforms();
            switch (id)
            {
                case "D0_Alley":
                    Cargo("Delivery", -3.3f, -1, 1.3f); Cargo("Recycling", 3.2f, 6, 1.2f); Pipes("Service", -3.5f, 6); break;
                case "D1_RedLight":
                    Bench("BusStop",-7,-5); Planter("StreetGreenA",-4,-5); Planter("StreetGreenB",10,4.5f);
                    Table("Terrace",4,5.3f); Cargo("ShopDelivery",17,-5,1.5f); Bench("LateNight",15,4.5f); break;
                case "I1_Bar":
                    Booth("BackBooth",3.8f,3.3f); Shelf("Supplies",-5.3f,-2.4f); Table("Corner",-3,-3); break;
                case "D2_Financial":
                    Planter("PromenadeA",-14,-4); Bench("PromenadeSeat",-10,-4.5f); Planter("PromenadeB",-5,-4.5f);
                    Bench("FountainSeat",6,6.5f); Planter("EastBoundary",18,4); Cargo("NewsStand",-17,5,1.4f); break;
                case "I2_Apartment":
                    Shelf("HallCabinet",5.5f,-4); Bench("HallSeat",-5,-2.1f); Table("LivingSide",7,2); break;
                case "I2_Coffee":
                    Booth("WindowSeat",-3.4f,-2.8f); Shelf("CoffeeStock",-4.7f,-.5f); Planter("EntrancePlant",4.6f,1.3f);
                    Booth("BackSeat",2.5f,3); Table("ReadingTable",-4.2f,-2.6f); break;
                case "D3_Industrial":
                    Cargo("LoadingBayA",-19,-7,2.6f); Cargo("LoadingBayB",-18,-11,2.3f);
                    Cargo("RepairBay",9,-10,2.8f); Pipes("PipeStorage",14,-10); Cargo("WarehouseYard",-4,7,2.4f); break;
                case "I3_Warehouse":
                    Shelf("StockRowA",-7,3.7f); Shelf("StockRowB",3.2f,0); Cargo("Packing",4,-3,1.8f);
                    Table("DispatchDesk",-5,0.5f); Cargo("RearStock",-1,5.5f,1.5f); break;
                case "I3_Basement":
                    Pipes("UtilityBank",-5.7f,2.7f); Shelf("Parts",0,-3.6f); Cargo("RepairParts",-5,-2,1.1f); break;
                case "I3_Rooftop":
                    Pipes("VentBank",-1,2.5f); Cargo("Maintenance",6.5f,0,1.5f); Planter("RoofEdge",-6,0.5f); break;
                case "D4_Residential":
                    Planter("GardenA",-7,-3.5f); Bench("CourtyardSeatA",-3,-4.5f); Planter("GardenB",2,-4.5f);
                    Bench("CourtyardSeatB",9,-4); Planter("GardenC",13,-3.5f); Cargo("RecycleBay",17,-5,1.4f); break;
                case "I4_YourHome":
                    Shelf("CaseArchive",-4.7f,3.8f); Booth("LivingCorner",4.3f,3.8f); Table("LowTable",3.8f,.5f); break;
                case "I4_VictimHome":
                    Shelf("FamilyBooks",-4.8f,-.7f); Bench("EntryCabinet",-3.3f,-3.5f); Planter("KitchenPlant",3,4.1f); break;
            }
            return id + ": " + group.childCount + " layout groups; skipped occupied: " + string.Join(",", skipped);
        }
        static Vector3[] Points(params float[] values)
        {
            var points = new Vector3[values.Length/2];
            for(int i=0;i<points.Length;i++) points[i]=new Vector3(values[i*2],0,values[i*2+1]);
            return points;
        }
        static Transform Reserve(string name,float x,float z,float width,float depth)
        {
            var center=new Vector3(x,.8f,z);
            // Preserve existing colliders, interactable approach space and the authored main route.
            foreach(var hit in Physics.OverlapBox(center,new Vector3(width/2+.1f,.7f,depth/2+.1f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
            { if(hit.enabled) { skipped.Add(name);return null; } }
            foreach(var target in group.root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(target is TestInteractable) && !(target is DetectiveDoorTeleport)) continue;
                var p=target.transform.position;
                if(Mathf.Abs(p.x-x)<width/2+1.2f && Mathf.Abs(p.z-z)<depth/2+1.2f) { skipped.Add(name);return null; }
            }
            foreach(var spawn in group.root.GetComponentsInChildren<Transform>())
                if(spawn.name.StartsWith("Spawn_") && Mathf.Abs(spawn.position.x-x)<width/2+.8f && Mathf.Abs(spawn.position.z-z)<depth/2+.8f) { skipped.Add(name);return null; }
            for(int i=1;i<route.Length;i++)
            {
                var d=route[i]-route[i-1];var p=new Vector3(x,0,z);
                var near=route[i-1]+d*Mathf.Clamp01(Vector3.Dot(p-route[i-1],d)/d.sqrMagnitude);
                if(Mathf.Abs(near.x-x)<width/2+(outdoor?1.25f:.8f) && Mathf.Abs(near.z-z)<depth/2+(outdoor?1.25f:.8f)) { skipped.Add(name);return null; }
            }
            var result=new GameObject(name).transform;result.SetParent(group,false);result.position=new Vector3(x,0,z);return result;
        }
        static void Part(Transform parent,string name,Vector3 local,Vector3 size,Color color)
        {
            var go=CreateCube(parent,name,parent.position+local,size,color);
            var modifier=go.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
        }
        static void Planter(string name,float x,float z)
        {
            var t=Reserve(name,x,z,1.8f,1);if(t==null)return;
            Part(t,"StoneTrough",new(0,.25f,0),new(1.8f,.5f,1),Stone);
            Part(t,"Foliage",new(0,.65f,0),new(1.5f,.5f,.75f),Leaf);
        }
        static void Bench(string name,float x,float z)
        {
            var t=Reserve(name,x,z,2,.7f);if(t==null)return;
            Part(t,"Seat",new(0,.45f,0),new(2,.16f,.7f),Wood);Part(t,"Back",new(0,.78f,.3f),new(2,.6f,.12f),Wood);
            Part(t,"LegA",new(-.7f,.2f,0),new(.15f,.4f,.6f),Metal);Part(t,"LegB",new(.7f,.2f,0),new(.15f,.4f,.6f),Metal);
        }
        static void Table(string name,float x,float z)
        {
            var t=Reserve(name,x,z,1.5f,1);if(t==null)return;
            Part(t,"Top",new(0,.65f,0),new(1.5f,.12f,1),Wood);Part(t,"Base",new(0,.3f,0),new(.5f,.6f,.5f),Metal);
            Part(t,"Book",new(-.35f,.74f,0),new(.35f,.06f,.45f),Stone);Part(t,"Cup",new(.4f,.8f,0),new(.15f,.22f,.15f),Stone);
        }
        static void Booth(string name,float x,float z)
        {
            var t=Reserve(name,x,z,2.2f,1);if(t==null)return;
            Part(t,"Seat",new(0,.35f,0),new(2.2f,.7f,1),Wood);Part(t,"Back",new(0,.85f,.4f),new(2.2f,.65f,.2f),Metal);
            Part(t,"CushionA",new(-.55f,.74f,0),new(.95f,.12f,.7f),Stone);Part(t,"CushionB",new(.55f,.74f,0),new(.95f,.12f,.7f),Stone);
        }
        static void Cargo(string name,float x,float z,float width)
        {
            var t=Reserve(name,x,z,width,1.4f);if(t==null)return;
            Part(t,"Pallet",new(0,.08f,0),new(width,.16f,1.4f),Wood);
            Part(t,"CrateA",new(-width*.23f,.55f,0),new(width*.42f,.9f,1.1f),Wood);
            Part(t,"CrateB",new(width*.23f,.42f,.12f),new(width*.4f,.65f,.85f),Stone);
            Part(t,"Strap",new(-width*.23f,1.01f,0),new(.12f,.025f,1.12f),Metal);
        }
        static void Shelf(string name,float x,float z)
        {
            var t=Reserve(name,x,z,1.8f,.65f);if(t==null)return;
            Part(t,"Left",new(-.85f,.85f,0),new(.1f,1.7f,.65f),Metal);Part(t,"Right",new(.85f,.85f,0),new(.1f,1.7f,.65f),Metal);
            for(int row=0;row<3;row++)
            {
                Part(t,"Shelf"+row,new(0,.15f+row*.55f,0),new(1.8f,.08f,.65f),Wood);
                for(int col=0;col<4;col++) Part(t,"Stock"+row+"_"+col,new(-.6f+col*.4f,.38f+row*.55f,0),new(.25f,.36f,.42f),col%2==0?Stone:Wood);
            }
        }
        static void Pipes(string name,float x,float z)
        {
            var t=Reserve(name,x,z,1.6f,1);if(t==null)return;
            for(int i=0;i<3;i++) Part(t,"Conduit"+i,new(-.5f+i*.5f,.5f,0),new(.3f,1,.8f),Metal);
        }
        static void MoveCoffee(Transform root)
        {
            var positions = new Dictionary<string,Vector3>
            {
                ["Counter"]=new(-2.2f,.5f,2.4f),["CoffeeMachine"]=new(-3,1.25f,2.4f),["Cup"]=new(-1.9f,1.12f,2.5f),
                ["Barista"]=new(-1,1,3.6f),["Table_C"]=new(-2.2f,.38f,-1.5f),["Chair_A"]=new(4.5f,.25f,.7f),
                ["Mug_B"]=new(3.3f,.84f,.8f),["Table_A"]=new(3.1f,.38f,.7f),["Table_B"]=new(3.3f,.38f,-1.5f),["Chair_B"]=new(4.7f,.25f,-1.5f),
                ["StoolHigh_A"]=new(-3.8f,.35f,1.2f),["StoolHigh_B"]=new(-2.8f,.35f,1.2f),
                ["Spawn_default"]=new(0,1,-1),["Spawn_from_D2_Financial"]=new(2.4f,1,-3)
            };
            var barista = root.Find("Barista").GetComponentInChildren<TestInteractable>(true);
            var baristaSo = new SerializedObject(barista);
            baristaSo.FindProperty("interactionRange").floatValue = 2.4f;
            baristaSo.ApplyModifiedPropertiesWithoutUndo();
            foreach(var pair in positions)
            { var t=root.Find(pair.Key);if(t!=null){Undo.RecordObject(t,"Rearrange cafe");t.position=pair.Value;EditorUtility.SetDirty(t);} }
        }
    }
}
