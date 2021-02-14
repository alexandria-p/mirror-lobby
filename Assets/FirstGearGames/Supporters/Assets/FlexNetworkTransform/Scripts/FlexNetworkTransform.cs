using FirstGearGames.Utilities.Objects;
using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{

    [DisallowMultipleComponent]
    public class FlexNetworkTransform : FlexNetworkTransformBase
    {
        #region Public.
        /// <summary>
        /// Transform to synchronize.
        /// </summary>
        public override Transform TargetTransform => base.transform;
        #endregion

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                /* If root then no need to send transform data as that's already
                * handled in the spawn message. */
                if (transform.root == null)
                    return base.OnSerialize(writer, initialState);

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
                /* If root then no need to read transform data as that's already
                * handled in the spawn message. */
                if (transform.root == null)
                {
                    base.OnDeserialize(reader, initialState);
                    return;
                }

                TargetTransform.SetPosition(base.UseLocalSpace, reader.ReadVector3());
                TargetTransform.SetRotation(base.UseLocalSpace, FlexNetworkTransformSerializers.ReadCompressedQuaternion(reader));
                TargetTransform.SetScale(reader.ReadVector3());
            }
            base.OnDeserialize(reader, initialState);
        }

    }
}

