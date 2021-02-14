using FirstGearGames.Utilities.Objects;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{


    /// <summary>
    /// A component to synchronize the position of child transforms of networked objects.
    /// <para>There must be a NetworkTransform on the root object of the hierarchy. There can be multiple NetworkTransformChild components on an object. This does not use physics for synchronization, it simply synchronizes the localPosition and localRotation of the child transform and lerps towards the recieved values.</para>
    /// </summary>
    public class FlexNetworkTransformChild : FlexNetworkTransformBase
    {
        #region Serialized.
        /// <summary>
        /// Transform to synchronize.
        /// </summary>
        [FormerlySerializedAs("Target")]
        [SerializeField]
        private Transform _target;
        #endregion

        #region Public.
        /// <summary>
        /// Transform to synchronize.
        /// </summary>
        public override Transform TargetTransform => _target;
        #endregion

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.WriteVector3(TargetTransform.GetPosition(base.UseLocalSpace));
                FlexNetworkTransformSerializers.WriteCompressedQuaternion(writer, TargetTransform.GetRotation(base.UseLocalSpace));
                writer.WriteVector3(TargetTransform.GetScale());
            }
            return base.OnSerialize(writer, initialState);
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                TargetTransform.SetPosition(base.UseLocalSpace, reader.ReadVector3());
                TargetTransform.SetRotation(base.UseLocalSpace, FlexNetworkTransformSerializers.ReadCompressedQuaternion(reader));
                TargetTransform.SetScale(reader.ReadVector3());
            }
            base.OnDeserialize(reader, initialState);
        }


    }

}

