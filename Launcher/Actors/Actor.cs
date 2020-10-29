﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace Launcher
{
    [Serializable]
    public class Actor
    {

        public uint TargetId { get; set; } //remove this
        public uint CurrentTarget { get; set; }

        public uint Id { get; set; }
        public uint NameId { get; set; }
        public uint ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassPath { get; set; }
        public uint ClassCode { get; set; }
        //public byte PropFlag { get; set; }
        public List<EventCondition> EventConditions { get; set; } = new List<EventCondition>();

        #region General
        public uint Size { get; set; }
        public uint Voice { get; set; }
        public ushort SkinColor { get; set; }
        #endregion

        #region Head       
        public ushort HairStyle { get; set; }
        public ushort HairColor { get; set; }
        public ushort HairHighlightColor { get; set; }
        public ushort HairVariation { get; set; }
        public ushort EyeColor { get; set; } //oddly not part of face bitfield values. Maybe it was added at a later time in development?
        #endregion        

        public Face Face { get; set; }
        public GearGraphics GearGraphics { get; set; } = new GearGraphics();
        public uint BaseModel { get; set; }
        public uint AppearanceCode { get; set; }

        public Position Position { get; set; } = new Position();
        public LuaParameters LuaParameters { get; set; }
        public uint[] Speeds { get; set; }

        public virtual void Spawn(Socket handler, ushort spawnType = 0, ushort isZoning = 0, int changingZone = 0, ushort actorIndex = 0)
        {
            Prepare(actorIndex);
            CreateActor(handler, 0x08);
            SetEventConditions(handler);
            SetSpeeds(handler, Speeds);
            SetPosition(handler, Position, spawnType, isZoning);
            SetAppearance(handler);
            SetName(handler);
            SetMainState(handler, MainState.Passive);
            SetSubState(handler);
            SetAllStatus(handler);
            SetIcon(handler);
            SetIsZoning(handler, false);
            LoadActorScript(handler, LuaParameters);
            ActorInit(handler);
        }

        public virtual void Prepare(ushort actorIndex = 0)
        {
            Zone zone = World.Instance.Zones.Find(x => x.Id == Position.ZoneId);
            Id = 4 << 28 | zone.Id << 19 | (uint)actorIndex; // 0x46700087;           

            LuaParameters = new LuaParameters
            {
                ActorName = GenerateActorName(actorIndex) /*+ "@" + Position.ZoneId.ToString("X3") + "00"*/,
                ClassName = ClassName,
                ClassCode = ClassCode
            };

            LuaParameters.Add(ClassPath + ClassName);
            LuaParameters.Add(false);
            LuaParameters.Add(false);
            LuaParameters.Add(false);
            LuaParameters.Add(false);
            LuaParameters.Add(false);
            LuaParameters.Add(ClassId);
            LuaParameters.Add(false);
            LuaParameters.Add(false);
            LuaParameters.Add((uint)0);
            LuaParameters.Add((uint)0);
            LuaParameters.Add("TEST");
        }

        public void SetEventConditions(Socket handler)
        {
            if (EventConditions.Count > 0) //not all actors have event conditions
            {
                foreach (var e in EventConditions)
                {
                    byte[] data = new byte[0x28];
                    byte[] conditionName = Encoding.ASCII.GetBytes(e.EventName);
                    int conditionNameLength = e.EventName.Length;

                    switch (e.Opcode)
                    {
                        case ServerOpcode.EmoteEvent:
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Priority), 0, data, 0, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.IsDisabled), 0, data, 0x1, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.EmoteId), 0, data, 0x2, sizeof(ushort));
                            Buffer.BlockCopy(conditionName, 0, data, 0x4, conditionNameLength);
                            break;
                        case ServerOpcode.PushEventCircle:
                            data = new byte[0x38];
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Radius), 0, data, 0, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(0x44533088), 0, data, 0x04, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(100.0f), 0, data, 0x08, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, data, 0x0c, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Direction), 0, data, 0x10, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, data, 0x11, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.IsSilent), 0, data, 0x12, sizeof(byte));
                            Buffer.BlockCopy(conditionName, 0, data, 0x13, conditionNameLength);
                            break;
                        case ServerOpcode.PushEvenFan:
                            data = new byte[0x40];
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Radius), 0, data, 0, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(Id), 0, data, 0x04, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Radius), 0, data, 0x08, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Direction), 0, data, 0x10, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Priority), 0, data, 0x11, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.IsSilent), 0, data, 0x12, sizeof(byte));
                            Buffer.BlockCopy(conditionName, 0, data, 0x13, conditionNameLength);
                            break;
                        case ServerOpcode.PushEventTriggerBox:
                            data = new byte[0x40];
                            Buffer.BlockCopy(BitConverter.GetBytes(e.BgObjectId), 0, data, 0, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.LayoutId), 0, data, 0x4, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.ActorId), 0, data, 0x8, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Direction), 0, data, 0x14, sizeof(byte));
                            Buffer.BlockCopy(conditionName, 0, data, 0x17, conditionNameLength);
                            Buffer.BlockCopy(Encoding.ASCII.GetBytes(e.ReactionName), 0, data, 0x38, e.ReactionName.Length);
                            break;
                        case ServerOpcode.NoticeEvent:
                        case ServerOpcode.TalkEvent:
                        default:
                            Buffer.BlockCopy(BitConverter.GetBytes(e.Priority), 0, data, 0, sizeof(byte));
                            Buffer.BlockCopy(BitConverter.GetBytes(e.IsDisabled), 0, data, 0x1, sizeof(byte));
                            Buffer.BlockCopy(conditionName, 0, data, 0x2, conditionNameLength);
                            break;
                    }

                    SendPacket(handler, e.Opcode, data);
                }
            }
        }

        public virtual void ActorInit(Socket handler)
        {
            //byte[] data = new byte[0x88];
            //SendPacket(handler, Opcode.ActorInit, data);
        }

        public virtual void LoadActorScript(Socket handler, LuaParameters luaParameters)
        {
            byte[] data = new byte[0x108];

            Buffer.BlockCopy(BitConverter.GetBytes(luaParameters.ClassCode), 0, data, 0, sizeof(uint));
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(luaParameters.ActorName), 0, data, 0x04, luaParameters.ActorName.Length);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(luaParameters.ClassName), 0, data, 0x24, luaParameters.ClassName.Length);

            LuaParameters.WriteParameters(ref data, luaParameters);

            SendPacket(handler, ServerOpcode.LoadClassScript, data);
        }

        public void SetIsZoning(Socket handler, bool isZoning = false)
        {
            byte[] data = new byte[0x08];
            data[0] = (byte)(isZoning ? 1 : 0);
            SendPacket(handler, ServerOpcode.SetIsZoning, data);
        }

        public void SetIcon(Socket handler)
        {
            byte[] data = new byte[0x08];
            /* will be properly implemented later */
            SendPacket(handler, ServerOpcode.SetIcon, data);
        }

        public void SetAllStatus(Socket handler)
        {
            byte[] data = new byte[0x28];
            /* will be properly implemented later */
            SendPacket(handler, ServerOpcode.SetAllStatus, data);
        }

        public void SetSubState(Socket handler, byte substate = 0)
        {
            /* will be properly implemented later */
            byte[] data = new byte[0x08];

            if (substate > 0)
                Buffer.BlockCopy(BitConverter.GetBytes(substate), 0, data, 0x03, 1);

            SendPacket(handler, ServerOpcode.SetSubState, data);
        }

        public void SetMainState(Socket handler, MainState state, byte type = 0)
        {
            byte[] data = new byte[0x08];
            data[0] = (byte)state;
            data[0x01] = type;
            SendPacket(handler, ServerOpcode.SetMainState, data);
        }

        public void CreateActor(Socket handler, byte code)
        {
            byte[] data = new byte[0x08];
            data[0] = code;
            SendPacket(handler, ServerOpcode.CreateActor, data);
        }

        public void SendUnknown(Socket handler) => SendPacket(handler, ServerOpcode.Unknown, new byte[0x18]);

        public void SetName(Socket handler, int isCustom = 0, byte[] customName = null)
        {
            byte[] data = new byte[0x28];

            if (customName != null)
            {
                Buffer.BlockCopy(customName, 0, data, 0x04, customName.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(isCustom), 0, data, 0, sizeof(int));
            }
            else
                Buffer.BlockCopy(BitConverter.GetBytes(NameId), 0, data, 0x00, sizeof(uint));

            SendPacket(handler, ServerOpcode.SetName, data);
        }

        public void SetPosition(Socket handler, Position position, ushort spawnType = 0, ushort isZonning = 0, int changingZone = 0)
        {
            byte[] data = new byte[0x28];

            if (changingZone == 0)
                changingZone = (int)Id;

            Buffer.BlockCopy(BitConverter.GetBytes(Id), 0, data, 0x04, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(position.X), 0, data, 0x08, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(position.Y), 0, data, 0x0c, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(position.Z), 0, data, 0x10, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(position.R), 0, data, 0x14, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(position.FloatingHeight), 0, data, 0x1c, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(spawnType), 0, data, 0x24, sizeof(ushort));
            Buffer.BlockCopy(BitConverter.GetBytes(isZonning), 0, data, 0x26, sizeof(ushort));

            SendPacket(handler, ServerOpcode.SetPosition, data);
        }

        public void SetSpeeds(Socket handler, uint[] value = null)
        {
            byte[] data = new byte[0x88];

            if (value == null) //load defaults
            {
                value = new uint[0x04];
                value[0] = 0;           //Stopped
                value[1] = 0x40000000;  //Walking speed
                value[2] = 0x40d00000; // 0x40a00000;  //Running speed
                value[3] = 0x40a00000;  //Acive
            }

            byte index = 0;
            for (int i = 0; i < value.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value[i]), 0, data, index, sizeof(uint));
                index += 0x04;
                data[index] = (byte)i;
                index += 0x04;
            }

            data[0x80] = 0x04; //only 4 states discovered so far.

            SendPacket(handler, ServerOpcode.SetSpeed, data);
        }

        public void SetAppearance(Socket handler)
        {
            byte[] data = new byte[0x108];

            Dictionary<uint, uint> AppearanceSlots = new Dictionary<uint, uint>
            {
                //slot number, value
                { 0x00, BaseModel },
                { 0x01, AppearanceCode },
                { 0x02, (uint)(SkinColor | HairColor << 10 | EyeColor << 20) },
                { 0x03, BitField.PrimitiveConversion.ToUInt32(Face) },
                { 0x04, (uint)(HairHighlightColor | HairStyle << 10) },
                { 0x05, Voice },
                { 0x06, GearGraphics.MainWeapon },
                { 0x07, GearGraphics.SecondaryWeapon },
                { 0x08, GearGraphics.SPMainWeapon },
                { 0x09, GearGraphics.SPSecondaryWeapon },
                { 0x0a, GearGraphics.Throwing },
                { 0x0b, GearGraphics.Pack },
                { 0x0c, GearGraphics.Pouch },
                { 0x0d, GearGraphics.Head },
                { 0x0e, GearGraphics.Body },
                { 0x0f, GearGraphics.Legs },
                { 0x10, GearGraphics.Hands },
                { 0x11, GearGraphics.Feet },
                { 0x12, GearGraphics.Waist },
                { 0x13, GearGraphics.Neck },
                { 0x14, GearGraphics.RightEar },
                { 0x15, GearGraphics.LeftEar },
                { 0x16, GearGraphics.Wrists },
                { 0x17, 0 },
                { 0x18, GearGraphics.LeftFinger },
                { 0x19, GearGraphics.RightFinger },
                { 0x1a, GearGraphics.RightIndex },
                { 0x1b, GearGraphics.LeftIndex }
            };

            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    foreach (var slot in AppearanceSlots)
                    {
                        writer.Write(slot.Value);
                        writer.Write(slot.Key);
                    }
                }
            }

            data[0x100] = (byte)AppearanceSlots.Count;

            SendPacket(handler, ServerOpcode.SetAppearance, data);
        }

        public void SendPacket(Socket handler, ServerOpcode opcode, byte[] data, uint sourceId = 0, uint targetId = 0)
        {
            GamePacket gamePacket = new GamePacket
            {
                Opcode = (ushort)opcode,
                Data = data
            };

            if (sourceId == 0)
                sourceId = Id;

            //Packet packet = new Packet(new SubPacket(gamePacket) { SourceId = sourceId > 0 ? sourceId : Id, TargetId = targetId > 0 ? targetId : TargetId });
            Packet packet = new Packet(new SubPacket(gamePacket) { SourceId = Id, TargetId = TargetId });
            handler.Send(packet.ToBytes());
        }

        public uint NewId()
        {
            Random rnd = new Random();
            byte[] id = new byte[0x4];
            rnd.NextBytes(id);
            return BitConverter.ToUInt32(id, 0);
        }

        public void DoEmote(Socket handler)
        {
            byte[] data = new byte[] { 0x00, 0xB0, 0x00, 0x05, 0x41, 0x29, 0x9B, 0x02, 0x6E, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Buffer.BlockCopy(BitConverter.GetBytes(Id), 0, data, 0x04, 4);
            SendPacket(handler, ServerOpcode.DoEmote, data);
        }

        /// <summary>
        /// This struct is used to keep data about event conditions for actors. Actors have a list of event conditions populated by specialized class' constructor. 
        /// </summary>
        [Serializable]
        public class EventCondition
        {
            public ServerOpcode Opcode { get; set; }
            public string EventName { get; set; }
            public ushort EmoteId { get; set; }
            public float Radius { get; set; }       //circle size
            public byte Priority { get; set; }      //unknown
            public byte IsDisabled { get; set; }    //0x1 won't fire event.
            public byte IsSilent { get; set; }      //0x1 do NOT lock UI and player.
            public byte Direction { get; set; }     //possible values: 0x11 leave circle, 0x1 enter circle.
            public uint ServerCodes { get; set; }

            //For BG objects
            public uint BgObjectId { get; set; }
            public uint LayoutId { get; set; }
            public uint ActorId { get; set; } = 0x4;
            public string ReactionName { get; set; }

            public byte Option1 { get; set; }
            public byte Option2 { get; set; }

            public EventCondition() { }
        }

        /// <summary>
        /// Converts a number to a base 63 string. This function was taken from Ioncannon's code, all credit goes to him. 
        /// </summary>
        /// <param name="number">The number to be converted.</param>
        /// <returns></returns>
        public string ToStringBase63(int number)
        {
            var lookup = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var startIndex = (int)Math.Floor(number / (double)lookup.Length);
            var secondDigit = lookup.Substring(startIndex, 1);
            var firstDigit = lookup.Substring(number % lookup.Length, 1);

            return secondDigit + firstDigit;
        }

        public string GenerateActorName(int actorNumber)
        {
            Zone zone = World.Instance.Zones.Find(x => x.Id == Position.ZoneId);            
            uint zoneId = zone.Id;
            uint privLevel = 0;

            //get actor zone name
            string zoneName = zone.MapName
                .Replace("Field", "Fld")
                .Replace("Dungeon", "Dgn")
                .Replace("Town", "Twn")
                .Replace("Battle", "Btl")
                .Replace("Test", "Tes")
                .Replace("Event", "Evt")
                .Replace("Ship", "Shp")
                .Replace("Office", "Ofc");

            //if (zone.ZoneType == ZoneType.Inn)
            //{
            //    zoneName = zoneName.Remove(zoneName.Length - 1, 1) + "P";
            //    //privLevel = ((PrivateArea)zone).GetPrivateAreaType();
            //}

            zoneName = Char.ToLowerInvariant(zoneName[0]) + zoneName.Substring(1);

            //Format Class Name
            string className = ClassName.Replace("Populace", "ppl")
                                        .Replace("Monster", "Mon")
                                        .Replace("Crowd", "Crd")
                                        .Replace("MapObj", "Map")
                                        .Replace("Object", "Obj")
                                        .Replace("Retainer", "Rtn")
                                        .Replace("Standard", "Std");

            className = Char.ToLowerInvariant(className[0]) + className.Substring(1);

            if(ClassName.Length > 6 && (ClassName.Length + (zoneName.Length + 4)) > 25)
                try{ className = className.Substring(0, 21 - zoneName.Length); }
                catch (ArgumentOutOfRangeException e) { Log.Instance.Error(e.Message); }
                        
            return string.Format("{0}_{1}_{2}@{3:X3}{4:X2}", className, zoneName, ToStringBase63(actorNumber), zoneId, privLevel);
        }

        public void GetBaseModel(byte id)
        {
            DataTable itemNames = GameData.Instance.GetGameData("tribe");
            DataRow[] selected = itemNames.Select("id = '" + id + "'");
            int model = (int)selected[0][1]; //had to do this as it was throwing cast error
            BaseModel = (uint)model;
        }
    }

    /// <summary>
    /// This class store and process all actor's parameters being passed to the game client Lua engine.
    /// </summary>
    [Serializable]
    public class LuaParameters
    {
        public string ActorName { get; set; }
        public string ClassName { get; set; }
        public uint ClassCode { get; set; }
        public List<KeyValuePair<byte, object>> List { get; set; } = new List<KeyValuePair<byte, object>>();

        /// <summary>
        /// Adds one single Lua parameter to the parameter list of the instanced obj.
        /// </summary>
        /// <param name="param">The parameter to be written.</param>
        public void Add(object param)
        {
            if (param is int)
                List.Add(new KeyValuePair<byte, object>(0, (int)param));
            else if (param is uint)
                List.Add(new KeyValuePair<byte, object>(0x01, (uint)param));
            else if (param is string)
                List.Add(new KeyValuePair<byte, object>(0x02, (string)param));
            else if (param is null)
                List.Add(new KeyValuePair<byte, object>(0x05, null));
            else if (param is byte)
                List.Add(new KeyValuePair<byte, object>(0xc, (byte)param));
            else if (param is bool)
            {
                if ((bool)param)
                    List.Add(new KeyValuePair<byte, object>(0x03, null));
                else
                    List.Add(new KeyValuePair<byte, object>(0x04, null));
            }
        }

        /// <summary>
        /// Writes all parameters in the instanced obj parameter list to the packet to be sent.
        /// </summary>
        /// <param name="data">A pointer to the packet buffer. </param>
        /// <param name="luaParameters">The list of parameters to be witten.</param>
        public static void WriteParameters(ref byte[] data, LuaParameters luaParameters)
        {
            //Write Params - using binary writer bc sizes, types, #items can vary.
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Seek(0x44, SeekOrigin.Begin); //points to the right position

                    foreach (var parameter in luaParameters.List)
                    {
                        if (parameter.Key == 0x01)
                            writer.Write((byte)0);
                        else
                            writer.Write(parameter.Key);

                        switch (parameter.Key)
                        {
                            case 0:
                                writer.Write(SwapEndian((int)parameter.Value));
                                break;
                            case 0x01:
                                writer.Write(SwapEndian((uint)parameter.Value));
                                break;
                            case 0x02:
                                string str = (string)parameter.Value;
                                writer.Write(Encoding.ASCII.GetBytes(str), 0, Encoding.ASCII.GetByteCount(str));
                                writer.Write((byte)0);
                                break;
                            case 0x05: //null
                                break;
                            case 0x06:
                                writer.Write(SwapEndian((uint)parameter.Value));
                                break;
                            case 0x07:
                                break;
                            case 0x09:
                                break;
                            case 0x0c:
                                writer.Write((byte)parameter.Value);
                                break;
                            case 0x1b:
                                break;
                            case 0x0f:
                                continue;
                        }
                    }

                    writer.Write((byte)0x0f);
                }
            }
        }

        #region Endian swappers
        public static ulong SwapEndian(ulong input)
        {
            return 0x00000000000000FF & (input >> 56) |
                   0x000000000000FF00 & (input >> 40) |
                   0x0000000000FF0000 & (input >> 24) |
                   0x00000000FF000000 & (input >> 8) |
                   0x000000FF00000000 & (input << 8) |
                   0x0000FF0000000000 & (input << 24) |
                   0x00FF000000000000 & (input << 40) |
                   0xFF00000000000000 & (input << 56);
        }

        public static uint SwapEndian(uint input)
        {
            return ((input >> 24) & 0xff) |
                   ((input << 8) & 0xff0000) |
                   ((input >> 8) & 0xff00) |
                   ((input << 24) & 0xff000000);
        }

        public static int SwapEndian(int input)
        {
            var inputAsUint = (uint)input;

            input = (int)
                (((inputAsUint >> 24) & 0xff) |
                 ((inputAsUint << 8) & 0xff0000) |
                 ((inputAsUint >> 8) & 0xff00) |
                 ((inputAsUint << 24) & 0xff000000));

            return input;
        }
        #endregion  
    }

    [Serializable]
    public struct Face
    {
        [BitField.BitfieldLength(5)]
        public uint Characteristics;
        [BitField.BitfieldLength(3)]
        public uint CharacteristicsColor;
        [BitField.BitfieldLength(6)]
        public uint Type;
        [BitField.BitfieldLength(2)]
        public uint Ears;
        [BitField.BitfieldLength(2)]
        public uint Mouth;
        [BitField.BitfieldLength(2)]
        public uint Features;
        [BitField.BitfieldLength(3)]
        public uint Nose;
        [BitField.BitfieldLength(3)]
        public uint EyeShape;
        [BitField.BitfieldLength(1)]
        public uint IrisSize;
        [BitField.BitfieldLength(3)]
        public uint EyeBrows;
        [BitField.BitfieldLength(2)]
        public uint Unknown;
    }

    public class CommandResult
    {
        public uint TargetId { get; set; }
        public ushort Amount { get; set; }
        public ushort TextId { get; set; }
        public uint EffectId { get; set; }
        public byte Param { get; set; }
        public byte Sequence { get; set; }

        public byte[] ToBytes()
        {
            byte[] result = new byte[0x0e];

            using (MemoryStream ms = new MemoryStream(result))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(TargetId);
                bw.Write(Amount);
                bw.Write(TextId);
                bw.Write(EffectId);
                bw.Write(Param);
                bw.Write(Sequence);
            }

            return result;
        }
    }
}
