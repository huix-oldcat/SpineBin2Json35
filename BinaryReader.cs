/**************************************************************************
    This file is part of SpineBin2Json.

    SpineBin2Json is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SpineBin2Json is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SpineBin2Json.  If not, see <https://www.gnu.org/licenses/>.
**************************************************************************/
using System.IO;
using System.Collections.Generic;

namespace SpineBin2Json35
{
    public partial class BinaryReader
    {
        #region Const
        public const int BONE_ROTATE = 0;
        public const int BONE_TRANSLATE = 1;
        public const int BONE_SCALE = 2;
        public const int BONE_SHEAR = 3;

        public const int SLOT_ATTACHMENT = 0;
        public const int SLOT_COLOR = 1;

        public const int PATH_POSITION = 0;
        public const int PATH_SPACING = 1;
        public const int PATH_MIX = 2;

        public const int CURVE_LINEAR = 0;
        public const int CURVE_STEPPED = 1;
        public const int CURVE_BEZIER = 2;
        #endregion Const

        Stream input;
        AtlasReader atlasReader;
        TextWriter warning;

        bool nonessential;
        List<string> bones = new List<string>();
        List<string> slots = new List<string>();
        List<string> iks = new List<string>();
        List<string> paths = new List<string>();
        List<string> transforms = new List<string>();
        List<string> skins = new List<string>();
        List<string> events = new List<string>();
        public BinaryReader(Stream input, AtlasReader atlasReader, TextWriter warning)
        {
            this.input = input;
            this.atlasReader = atlasReader;
            this.warning = warning;
        }

        public object Convert()
        {
            var root = new Dictionary<string, object>();

            root["skeleton"] = ReadMajor();

            var bone_map = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                bone_map.Add(ReadBone());
            }
            root["bones"] = bone_map;

            var slot_map = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                slot_map.Add(ReadSlot());
            }
            root["slots"] = slot_map;

            var ik_map = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                ik_map.Add(ReadIk());
            }
            root["ik"] = ik_map;

            var tr_map = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                tr_map.Add(ReadTransform());
            }
            root["transform"] = tr_map;

            var path_map = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                path_map.Add(ReadPath());
            }
            root["path"] = path_map;

            var skins_map = new Dictionary<string, object>();
            skins_map["default"] = ReadSkin();
            skins.Add("default");
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string skin_name = ReadString(input);
                skins.Add(skin_name);
                skins_map[skin_name] = ReadSkin();
            }
            root["skins"] = skins_map;

            var events_map = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string name = ReadString(input);
                events.Add(name);
                var e1 = new Dictionary<string, object>();
                e1["int"] = ReadVarint(input, false);
                e1["float"] = ReadFloat(input);
                e1["string"] = ReadString(input);
                events_map[name] = e1;
            }
            root["events"] = events_map;

            var an_map = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string name = ReadString(input);
                an_map[name] = ReadAnimation();
            }
            root["animations"] = an_map;

            return root;
        }

        /*static void ReadCurve (Dictionary<string, Object> valueMap, CurveTimeline timeline, int frameIndex) {
            if (!valueMap.ContainsKey("curve"))
                return;
            Object curveObject = valueMap["curve"];
            if (curveObject.Equals("stepped"))
                timeline.SetStepped(frameIndex);
            else {
                var curve = curveObject as List<Object>;
                if (curve != null)
                    timeline.SetCurve(frameIndex, (float)curve[0], (float)curve[1], (float)curve[2], (float)curve[3]);
            }
        } */
        void ReadCurve(Dictionary<string, object> root)
        {
            switch (input.ReadByte())
            {
                case CURVE_STEPPED:
                    root["curve"] = "stepped";
                    break;
                case CURVE_BEZIER:
                    root["curve"] = new float[4] { ReadFloat(input), ReadFloat(input), ReadFloat(input), ReadFloat(input) };
                    break;
            }
        }
        /*private void ReadCurve (Stream input, int frameIndex, CurveTimeline timeline) {
            switch (input.ReadByte()) {
            case CURVE_STEPPED:
                timeline.SetStepped(frameIndex);
                break;
            case CURVE_BEZIER:
                timeline.SetCurve(frameIndex, ReadFloat(input), ReadFloat(input), ReadFloat(input), ReadFloat(input));
                break;
            }
        } */


        object ReadAnimation()
        {
            var root = new Dictionary<string, object>();

            var slotsMap = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)// Slot timelines
            {
                string slotName = slots[ReadVarint(input, true)];
                var timelineMap = new Dictionary<string, object>();
                slotsMap[slotName] = timelineMap;
                for (int ii = 0, nn = ReadVarint(input, true); ii < nn; ii++)
                {
                    int timelineType = input.ReadByte();
                    int frameCount = ReadVarint(input, true);
                    switch (timelineType)
                    {
                        case SLOT_ATTACHMENT:
                            {
                                if (timelineMap.ContainsKey("attachment") == false)
                                {
                                    timelineMap["attachment"] = new List<object>();
                                }
                                var timelines = timelineMap["attachment"] as List<object>;
                                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                                {
                                    var details = new Dictionary<string, object>();
                                    details["time"] = ReadFloat(input);
                                    details["name"] = ReadString(input);
                                    timelines.Add(details);
                                }
                                break;
                            }
                        case SLOT_COLOR:
                            {
                                if (timelineMap.ContainsKey("color") == false)
                                {
                                    timelineMap["color"] = new List<object>();
                                }
                                var timelines = timelineMap["color"] as List<object>;
                                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                                {
                                    var details = new Dictionary<string, object>();
                                    details["time"] = ReadFloat(input);
                                    details["color"] = ColorTranslator.ColorToString(ReadInt(input), 8);
                                    if (frameIndex < frameCount - 1) ReadCurve(details);
                                    timelines.Add(details);
                                }
                                break;
                            }
                            // case SLOT_TWO_COLOR:
                            //     {
                            //         if (timelineMap.ContainsKey("twoColor") == false)
                            //         {
                            //             timelineMap["twoColor"] = new List<object>();
                            //         }
                            //         var timelines = timelineMap["twoColor"] as List<object>;
                            //         for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                            //         {
                            //             var details = new Dictionary<string, object>();
                            //             details["time"] = ReadFloat(input);
                            //             details["light"] = ColorTranslator.RgbaToString(ReadInt(input));
                            //             details["dark"] = ColorTranslator.RgbToString(ReadInt(input));
                            //             if (frameIndex < frameCount - 1) ReadCurve(details);
                            //             timelines.Add(details);
                            //         }
                            //         break;
                            //     }
                    }
                }
            }
            root["slots"] = slotsMap;

            var bonesMap = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string boneName = bones[ReadVarint(input, true)];
                var timelineMap = new Dictionary<string, object>();
                bonesMap[boneName] = timelineMap;
                for (int ii = 0, nn = ReadVarint(input, true); ii < nn; ii++)
                {
                    int timelineType = input.ReadByte();
                    int frameCount = ReadVarint(input, true);
                    switch (timelineType)
                    {
                        case BONE_ROTATE:
                            {
                                if (timelineMap.ContainsKey("rotate") == false)
                                {
                                    timelineMap["rotate"] = new List<object>();
                                }
                                var timelines = timelineMap["rotate"] as List<object>;
                                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                                {
                                    var details = new Dictionary<string, object>();
                                    details["time"] = ReadFloat(input);
                                    details["angle"] = ReadFloat(input);
                                    if (frameIndex < frameCount - 1) ReadCurve(details);
                                    timelines.Add(details);
                                }
                                break;
                            }
                        case BONE_TRANSLATE:
                        case BONE_SCALE:
                        case BONE_SHEAR:
                            {
                                string type = "";
                                if (timelineType == BONE_SCALE)
                                    type = "scale";
                                else if (timelineType == BONE_SHEAR)
                                    type = "shear";
                                else
                                    type = "translate";
                                if (type == "shear") warning.WriteLine($"Dragonbones cannot support BONE_SHEAR");
                                if (timelineMap.ContainsKey(type) == false)
                                {
                                    timelineMap[type] = new List<object>();
                                }
                                var timelines = timelineMap[type] as List<object>;
                                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                                {
                                    var details = new Dictionary<string, object>();
                                    details["time"] = ReadFloat(input);
                                    details["x"] = ReadFloat(input);
                                    details["y"] = ReadFloat(input);
                                    if (frameIndex < frameCount - 1) ReadCurve(details);
                                    timelines.Add(details);
                                }
                                break;
                            }
                    }
                }
            }
            root["bones"] = bonesMap;

            var ikMap = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                int index = ReadVarint(input, true);
                int frameCount = ReadVarint(input, true);
                var e1 = new List<object>();
                ikMap[iks[index]] = e1;
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var details = new Dictionary<string, object>();
                    e1.Add(details);
                    details["time"] = ReadFloat(input);
                    details["mix"] = ReadFloat(input);
                    details["bendPositive"] = ReadSByte(input) == 1;
                    // details["compress"] = ReadBoolean(input);
                    // details["stretch"] = ReadBoolean(input);
                    if (frameIndex < frameCount - 1) ReadCurve(details);
                }
            }
            root["ik"] = ikMap;

            var trMap = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                int index = ReadVarint(input, true);
                int frameCount = ReadVarint(input, true);
                var e1 = new List<object>();
                trMap[transforms[index]] = e1;
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var details = new Dictionary<string, object>();
                    e1.Add(details);
                    details["time"] = ReadFloat(input);
                    details["rotateMix"] = ReadFloat(input);
                    details["translateMix"] = ReadFloat(input);
                    details["scaleMix"] = ReadFloat(input);
                    details["shearMix"] = ReadFloat(input);
                    if (frameIndex < frameCount - 1) ReadCurve(details);
                }
            }
            root["transform"] = trMap;

            var paMap = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string pathName = paths[ReadVarint(input, true)];
                var timelineMap = new Dictionary<string, object>();
                paMap[pathName] = timelineMap;
                for (int ii = 0, nn = ReadVarint(input, true); ii < nn; ii++)
                {
                    int timelineType = ReadSByte(input);
                    int frameCount = ReadVarint(input, true);
                    switch (timelineType)
                    {

                        case PATH_POSITION:
                        case PATH_SPACING:
                            {
                                string type = "";
                                if (timelineType == PATH_SPACING)
                                    type = "spacing";
                                else
                                    type = "position";
                                var timelines = timelineMap[type] as List<object>;
                                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                                {
                                    var details = new Dictionary<string, object>();
                                    details["time"] = ReadFloat(input);
                                    details[type] = ReadFloat(input);
                                    if (frameIndex < frameCount - 1) ReadCurve(details);
                                    timelines.Add(details);
                                }
                                break;
                            }
                        case PATH_MIX:
                            {
                                var timelines = timelineMap["mix"] as List<object>;
                                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                                {
                                    var details = new Dictionary<string, object>();
                                    details["time"] = ReadFloat(input);
                                    details["rotateMix"] = ReadFloat(input);
                                    details["translateMix"] = ReadFloat(input);
                                    if (frameIndex < frameCount - 1) ReadCurve(details);
                                    timelines.Add(details);
                                }
                                break;
                            }
                    }
                }
            }
            root["path"] = paMap;

            var deMap = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string skinName = skins[ReadVarint(input, true)];
                var slotMap = new Dictionary<string, object>();
                deMap[skinName] = slotMap;
                for (int ii = 0, nn = ReadVarint(input, true); ii < nn; ii++)
                {
                    string slotName = slots[ReadVarint(input, true)];
                    var attachmentMap = new Dictionary<string, object>();
                    slotMap[slotName] = attachmentMap;
                    for (int iii = 0, nnn = ReadVarint(input, true); iii < nnn; iii++)
                    {
                        string attachmentName = ReadString(input);
                        var values = new List<object>();
                        attachmentMap[attachmentName] = values;
                        int frameCount = ReadVarint(input, true);
                        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                        {
                            var details = new Dictionary<string, object>();
                            values.Add(details);
                            details["time"] = ReadFloat(input);
                            int end = ReadVarint(input, true);
                            if (end != 0)
                            {
                                int start = ReadVarint(input, true);
                                end += start;
                                details["offset"] = start;
                                var vertices = new List<float>();
                                for (int v = start; v < end; v++)
                                    vertices.Add(ReadFloat(input));
                                details["vertices"] = vertices;
                            }
                            if (frameIndex < frameCount - 1) ReadCurve(details);
                        }
                    }
                }
            }
            root["deform"] = deMap;

            var doMap = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                var details = new Dictionary<string, object>();
                doMap.Add(details);
                details["time"] = ReadFloat(input);
                var e1 = new List<object>();
                details["offsets"] = e1;
                for (int ii = 0, nn = ReadVarint(input, true); ii < nn; ii++)
                {
                    var e2 = new Dictionary<string, object>();
                    e1.Add(e2);
                    e2["slot"] = slots[ReadVarint(input, true)];
                    e2["offset"] = ReadVarint(input, true);
                }
            }
            root["drawOrder"] = doMap;

            for (int i = 0, n = ReadVarint(input, true); i < n; i++)//JOJO!
            {
                /*float time = ReadFloat(input);
                    EventData eventData = skeletonData.events.Items[ReadVarint(input, true)];
                    Event e = new Event(time, eventData) {
                        Int = ReadVarint(input, false),
                        Float = ReadFloat(input),
                        String = ?  : eventData.String
                    };
                    if (e.data.AudioPath != null) {
                        e.volume = ReadFloat(input);
                        e.balance = ReadFloat(input);
                    }
                    timeline.SetFrame(i, e); */
                _ = ReadFloat(input);
                string name = events[ReadVarint(input, true)];
                _ = ReadVarint(input, false);
                _ = ReadFloat(input);
                if (ReadBoolean(input)) _ = ReadString(input);
                // if (eventsHasAudio[name])
                // {
                //     _ = ReadFloat(input);
                //     _ = ReadFloat(input);
                // }
            }
            return root;
        }

        enum AttachmentType
        {
            Region, Boundingbox, Mesh, Linkedmesh, Path//, Point, Clipping
        }

        object ReadAttachment(string oname)
        {
            var root = new Dictionary<string, object>();
            string name = ReadString(input);
            if (name == null) name = oname;
            root["name"] = name;
            AttachmentType type = (AttachmentType)input.ReadByte();
            root["path"] = name;
            switch (type)
            {
                case AttachmentType.Region:
                    {
                        root["type"] = "region";
                        root["path"] = ReadString(input);
                        root["rotation"] = ReadFloat(input);
                        root["x"] = ReadFloat(input);
                        root["y"] = ReadFloat(input);
                        root["scaleX"] = ReadFloat(input)*2;
                        root["scaleY"] = ReadFloat(input)*2;
                        root["width"] = ReadFloat(input);
                        root["height"] = ReadFloat(input);
                        root["color"] = ReadInt(input);
                        return root;
                    }
                case AttachmentType.Boundingbox:
                    {
                        warning.WriteLine($"Dragonbones may not support bounding box attachment {root["name"]}");
                        root["type"] = "boundingbox";
                        int vertexCount = ReadVarint(input, true);
                        root["vertexCount"] = vertexCount;
                        ReadVertices(vertexCount, root);
                        if (nonessential) ReadInt(input);
                        return root;
                    }
                case AttachmentType.Mesh:
                    {
                        root["type"] = "mesh";
                        root["path"] = ReadString(input);
                        root["color"] = ColorTranslator.ColorToString(ReadInt(input), 8);
                        int vertexCount = ReadVarint(input, true);
                        root["uvs"] = ReadFloatArray(input, vertexCount << 1, 1);
                        root["triangles"] = ReadShortArray(input);
                        ReadVertices(vertexCount, root);
                        int hullLength = ReadVarint(input, true);
                        root["hull"] = hullLength;
                        if (nonessential)
                        {
                            root["edges"] = ReadShortArray(input);
                            root["width"] = ReadFloat(input);
                            root["height"] = ReadFloat(input);
                        }
                        else
                        {
                            root["edges"] = GetDefaultEdges(hullLength << 1);
                            root["width"] = atlasReader.size[(root["path"] as string) == null ? (root["name"] as string) : (root["path"] as string)][0];
                            root["height"] = atlasReader.size[(root["path"] as string) == null ? (root["name"] as string) : (root["path"] as string)][1];
                        }
                        return root;
                    }
                case AttachmentType.Linkedmesh:
                    {
                        root["type"] = "linkedmesh";
                        root["path"] = ReadString(input);
                        root["color"] = ColorTranslator.ColorToString(ReadInt(input), 8);
                        root["skin"] = ReadString(input);
                        root["parent"] = ReadString(input);
                        root["deform"] = ReadBoolean(input);
                        if (nonessential)
                        {
                            root["width"] = ReadFloat(input);
                            root["height"] = ReadFloat(input);
                        }
                        return root;
                    }
                case AttachmentType.Path:
                    {
                        warning.WriteLine($"Dragonbones may not support path attachment {root["name"]}");
                        root["type"] = "path";
                        root["closed"] = ReadBoolean(input);
                        root["constantSpeed"] = ReadBoolean(input);
                        int vertexCount = ReadVarint(input, true);
                        root["vertexCount"] = vertexCount;
                        ReadVertices(vertexCount, root);
                        float[] lengths = new float[vertexCount / 3];
                        for (int i = 0, n = lengths.Length; i < n; i++)
                            lengths[i] = ReadFloat(input);
                        root["lengths"] = lengths;
                        if (nonessential) _ = ReadInt(input);
                        return root;
                    }
                    // case AttachmentType.Point:
                    //     {
                    //         root["rotation"] = ReadFloat(input);
                    //         root["x"] = ReadFloat(input);
                    //         root["y"] = ReadFloat(input);
                    //         if (nonessential) _ = ReadInt(input);
                    //         return root;
                    //     }
                    // case AttachmentType.Clipping:
                    //     {
                    //         root["end"] = slots[ReadVarint(input, true)];
                    //         int vertexCount = ReadVarint(input, true);
                    //         root["vertexCount"] = vertexCount;
                    //         ReadVertices(vertexCount, root);
                    //         if (nonessential) _ = ReadInt(input);
                    //         return root;
                    //     }
            }
            return null;
        }

        void ReadVertices(int vertexCount, Dictionary<string, object> root)
        {
            if (!ReadBoolean(input))
            {
                root["vertices"] = ReadFloatArray(input, vertexCount << 1, 1);
                return;
            }
            var c1 = new List<float>();
            for (int i = 0; i < vertexCount; i++)
            {
                int boneCount = ReadVarint(input, true);
                c1.Add(boneCount);
                for (int ii = 0; ii < boneCount; ii++)
                {
                    c1.Add(ReadVarint(input, true));
                    c1.Add(ReadFloat(input));
                    c1.Add(ReadFloat(input));
                    c1.Add(ReadFloat(input));
                }
            }
            root["vertices"] = c1;
            return;
        }

        object ReadSkin()
        {
            var slots_map = new Dictionary<string, object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++)
            {
                string slot_name = slots[ReadVarint(input, true)];
                var attachments_map = new Dictionary<string, object>();
                slots_map[slot_name] = attachments_map;
                for (int ii = 0, nn = ReadVarint(input, true); ii < nn; ii++)
                {
                    string attachment_name = ReadString(input);
                    attachments_map[attachment_name] = ReadAttachment(attachment_name);
                }
            }
            return slots_map;
        }

        static readonly string[] positionMode = { "fixed", "percent" };
        static readonly string[] spacingMode = { "length", "fixed", "percent" };
        static readonly string[] rotateMode = { "tangent", "chain", "chainScale" };

        object ReadPath()
        {
            var root = new Dictionary<string, object>();
            var warpper = new DictionaryWarpper(root);
            root["name"] = ReadString(input);
            warning.WriteLine($"Dragonbones cannot support path constraint {root["name"]}");
            paths.Add(root["name"] as string);
            root["order"] = ReadVarint(input, true);
            var c1 = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++) c1.Add(bones[ReadVarint(input, true)]);
            root["bones"] = c1;
            root["target"] = bones[ReadVarint(input, true)];
            //root["positionMode"] = positionMode[ReadVarint(input, true)];
            warpper.SetValue("positionMode", positionMode[ReadVarint(input, true)], "percent");
            //root["spacingMode"] = spacingMode[ReadVarint(input, true)];
            warpper.SetValue("spacingMode", spacingMode[ReadVarint(input, true)], "length");
            //root["rotateMode"] = rotateMode[ReadVarint(input, true)];
            warpper.SetValue("rotateMode", rotateMode[ReadVarint(input, true)], "tangent");
            //root["rotation"] = ReadFloat(input);
            warpper.SetValue("rotation", ReadFloat(input), (float)0);
            //root["position"] = ReadFloat(input);
            warpper.SetValue("position", ReadFloat(input), (float)0);
            //root["spacing"] = ReadFloat(input);
            warpper.SetValue("spacing", ReadFloat(input), (float)0);
            //root["rotateMix"] = ReadFloat(input);
            warpper.SetValue("rotateMix", ReadFloat(input), (float)1);
            //root["translateMix"] = ReadFloat(input);
            warpper.SetValue("translateMix", ReadFloat(input), (float)1);
            return root;
        }

        object ReadTransform()
        {
            var root = new Dictionary<string, object>();
            var warpper = new DictionaryWarpper(root);
            root["name"] = ReadString(input);
            warning.WriteLine($"Dragonbones cannot support transform constraint {root["name"]}");
            transforms.Add(root["name"] as string);
            root["order"] = ReadVarint(input, true);
            var c1 = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++) c1.Add(bones[ReadVarint(input, true)]);
            root["bones"] = c1;
            root["target"] = bones[ReadVarint(input, true)];
            //root["rotation"] = ReadFloat(input);
            warpper.SetValue("rotation", ReadFloat(input), (float)0);
            //root["x"] = ReadFloat(input);
            warpper.SetValue("x", ReadFloat(input), (float)0);
            //root["y"] = ReadFloat(input);
            warpper.SetValue("y", ReadFloat(input), (float)0);
            //root["scaleX"] = ReadFloat(input);
            warpper.SetValue("scaleX", ReadFloat(input), (float)0);
            //root["scaleY"] = ReadFloat(input);
            warpper.SetValue("scaleY", ReadFloat(input), (float)0);
            //root["shearY"] = ReadFloat(input);
            warpper.SetValue("shearY", ReadFloat(input), (float)0);
            //root["rotateMix"] = ReadFloat(input);
            warpper.SetValue("rotateMix", ReadFloat(input), (float)1);
            //root["translateMix"] = ReadFloat(input);
            warpper.SetValue("translateMix", ReadFloat(input), (float)1);
            //root["scaleMix"] = ReadFloat(input);
            warpper.SetValue("scaleMix", ReadFloat(input), (float)1);
            //root["shearMix"] = ReadFloat(input);
            warpper.SetValue("shearMix", ReadFloat(input), (float)1);
            return root;
        }

        object ReadIk()
        {
            var root = new Dictionary<string, object>();
            var warpper = new DictionaryWarpper(root);
            root["name"] = ReadString(input);
            iks.Add(root["name"] as string);
            root["order"] = ReadVarint(input, true);
            var c1 = new List<object>();
            for (int i = 0, n = ReadVarint(input, true); i < n; i++) c1.Add(bones[ReadVarint(input, true)]);
            root["bones"] = c1;
            root["target"] = bones[ReadVarint(input, true)];
            //root["mix"] = ReadFloat(input);
            warpper.SetValue("mix", ReadFloat(input), (float)1);
            //root["bendPositive"] = ReadSByte(input) == 1;
            warpper.SetValue("bendPositive", ReadSByte(input) == 1, true);
            return root;
        }

        static readonly string[] BlendMode = {
            "Normal",
            "Additive",
            "Multiply",
            "Screen"
        };

        object ReadSlot()
        {
            var root = new Dictionary<string, object>();
            var warpper = new DictionaryWarpper(root);
            root["name"] = ReadString(input);
            slots.Add(root["name"] as string);
            root["bone"] = bones[ReadVarint(input, true)];
            //root["color"] = ColorTranslator.RgbaToString(ReadInt(input));
            warpper.SetValue("color", ColorTranslator.ColorToString(ReadInt(input), 8), "FFFFFFFF");
            root["attachment"] = ReadString(input);
            //int i = ReadVarint(input, true);
            //root["blend"] = BlendMode[i];
            warpper.SetValue("blend", BlendMode[ReadVarint(input, true)], BlendMode[0]);
            return root;
        }

        static readonly string[] TransformModeValues = {
            "Normal",
            "OnlyTranslation",
            "NoRotationOrReflection",
            "NoScale",
            "NoScaleOrReflection"
        };

        object ReadBone()
        {
            var root = new Dictionary<string, object>();
            var warpper = new DictionaryWarpper(root);
            root["name"] = ReadString(input);
            if (bones.Count != 0) root["parent"] = bones[ReadVarint(input, true)];
            bones.Add(root["name"] as string);
            //root["rotation"] = ReadFloat(input);
            warpper.SetValue("rotation", ReadFloat(input), (float)0);
            //root["x"] = ReadFloat(input);
            warpper.SetValue("x", ReadFloat(input), (float)0);
            //root["y"] = ReadFloat(input);
            warpper.SetValue("y", ReadFloat(input), (float)0);
            //root["scaleX"] = ReadFloat(input);
            warpper.SetValue("scaleX", ReadFloat(input), (float)1);
            //root["scaleY"] = ReadFloat(input);
            warpper.SetValue("scaleY", ReadFloat(input), (float)1);
            //root["shearX"] = ReadFloat(input);
            warpper.SetValue("shearX", ReadFloat(input), (float)0);
            //root["shearY"] = ReadFloat(input);
            warpper.SetValue("shearY", ReadFloat(input), (float)0);
            //root["length"] = ReadFloat(input);
            warpper.SetValue("length", ReadFloat(input), (float)0);
            //root["transform"] = TransformModeValues[ReadVarint(input, true)];
            warpper.SetValue("transform", TransformModeValues[ReadVarint(input, true)], TransformModeValues[0]);
            if (nonessential) ReadInt(input);
            return root;
        }

        object ReadMajor()
        {
            Dictionary<string, object> root = new Dictionary<string, object>();
            root["hash"] = ReadString(input);
            root["spine"] = ReadString(input);
            System.Console.Write("Spine version: ");
            System.Console.BackgroundColor = System.ConsoleColor.DarkGreen;
            System.Console.ForegroundColor = System.ConsoleColor.White;
            System.Console.WriteLine(root["spine"]);
            System.Console.BackgroundColor = System.ConsoleColor.Black;
            System.Console.ForegroundColor = System.ConsoleColor.Gray;
            root["width"] = ReadFloat(input);
            root["height"] = ReadFloat(input);
            nonessential = ReadBoolean(input);
            if (nonessential)
            {
                root["fps"] = ReadFloat(input);
                root["images"] = ReadString(input);
            }
            return root;
        }

        class DictionaryWarpper
        {
            public Dictionary<string, object> o;
            public DictionaryWarpper(Dictionary<string, object> o)
            {
                this.o = o;
            }
            public void SetValue(string name, object val, object d)
            {
                if (val.GetType() == typeof(float))
                {
                    SetFloatValue(name, (float)val, (float)d);
                    return;
                }
                if (val.Equals(d))
                {
                    return;
                }
                o[name] = val;
            }
            public void SetValue(string name, object val)
            {
                o[name] = val;
            }

            private void SetFloatValue(string name, float val, float d)
            {
                if (System.Math.Abs(val - d) < 0.00001)
                {
                    return;
                }
                o[name] = val;
            }
        }
    }
}