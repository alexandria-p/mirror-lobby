using Mirror;
using FirstGearGames.Utilities.Networks;
using System;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    /// <summary>
    /// Data received on server from clients when using Client Authoritative movement.
    /// </summary>
    public class ReceivedClientData
    {
        #region Types.
        public enum DataTypes
        {
            Interval = 0,
            Teleport = 1
        }
        #endregion
        public ReceivedClientData() { }
        public ReceivedClientData(DataTypes dataType, bool localSpace, TransformSyncData data)
        {
            DataType = dataType;
            LocalSpace = localSpace;
            Data = data;
        }

        public DataTypes DataType;
        public bool LocalSpace;
        public TransformSyncData Data;
    }

    [System.Serializable, System.Flags]
    public enum Axes : int
    {
        X = 1,
        Y = 2,
        Z = 4
    }

    /// <summary>
    /// Transform properties which need to be synchronized.
    /// </summary>
    [System.Flags]
    public enum SyncProperties : byte
    {
        None = 0,
        //Position included.
        Position = 1,
        //Rotation included.
        Rotation = 2,
        //Scale included.
        Scale = 4,
        //Indicates packet is sequenced, generally for UDP.
        Sequenced = 8,
        //Indicates transform did not move.
        Settled = 16
    }

    /// <summary>
    /// Using strongly typed for performance.
    /// </summary>
    public static class EnumContains
    {
        /// <summary>
        /// Returns if a SyncProperties Whole contains Part.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public static bool SyncPropertiesContains(SyncProperties whole, SyncProperties part)
        {
            return (whole & part) == part;
        }

        /// <summary>
        /// Returns if a Axess Whole contains Part.
        /// </summary>
        /// <param name="whole"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public static bool AxesContains(Axes whole, Axes part)
        {
            return (whole & part) == part;
        }
    }


    /// <summary>
    /// Container holding latest transform values.
    /// </summary>
    [System.Serializable]
    public class TransformSyncData
    {
        public TransformSyncData() { }
        public TransformSyncData(byte syncProperties, uint sequenceId, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SyncProperties = syncProperties;
            SequenceId = sequenceId;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public byte SyncProperties;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public uint SequenceId = 0;

        public float TransitionRate;
    }

    public static class FlexNetworkTransformSerializers
    {
        /// <summary>
        /// Writes TransformSyncData into a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="syncData"></param>
        public static void WriteTransformSyncData(this NetworkWriter writer, TransformSyncData syncData)
        {
            //SyncProperties.
            SyncProperties sp = (SyncProperties)syncData.SyncProperties;
            writer.WriteByte(syncData.SyncProperties);
            //SequenceId.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Sequenced))
                writer.WriteUInt32(syncData.SequenceId);
            //Position.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
                writer.WriteVector3(syncData.Position);
            //Rotation.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                WriteCompressedQuaternion(writer, syncData.Rotation);
            //Scale.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
                writer.WriteVector3(syncData.Scale);
        }

        /// <summary>
        /// Converts reader data into a new TransformSyncData.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static TransformSyncData ReadTransformSyncData(this NetworkReader reader)
        {
            SyncProperties sp = (SyncProperties)reader.ReadByte();

            TransformSyncData syncData = new TransformSyncData();
            syncData.SyncProperties = (byte)sp;

            //SequenceId.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Sequenced))
                syncData.SequenceId = reader.ReadUInt32();
            //Position.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
                syncData.Position = reader.ReadVector3();
            //Rotation.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
                syncData.Rotation = ReadCompressedQuaternion(reader);
            //scale.
            if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
                syncData.Scale = reader.ReadVector3();

            return syncData;
        }

        /// <summary>
        /// Reads a compressed quaternion from a reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Quaternion ReadCompressedQuaternion(NetworkReader reader)
        {
            byte largest = reader.ReadByte();
            short a = 0, b = 0, c = 0;
            if (!Quaternions.UseLargestOnly(largest))
            {
                a = reader.ReadInt16();
                b = reader.ReadInt16();
                c = reader.ReadInt16();
            }
            return Quaternions.DecompressQuaternion(largest, a, b, c);
        }

        /// <summary>
        /// Writes a compressed quaternion to a writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="rotation"></param>
        public static void WriteCompressedQuaternion(NetworkWriter writer, Quaternion rotation)
        {
            byte largest;
            short a, b, c;
            Quaternions.CompressQuaternion(rotation, out largest, out a, out b, out c);
            writer.WriteByte(largest);
            if (!Quaternions.UseLargestOnly(largest))
            {
                writer.WriteInt16(a);
                writer.WriteInt16(b);
                writer.WriteInt16(c);
            }
        }
    }


}