using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class OrientationModeProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "mode", Enum.GetValues(typeof(Orient.Mode)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Orientation", variantProvider = typeof(OrientationModeProvider))]
    class Orient : VFXBlock
    {
        public enum Mode
        {
            FaceCameraPlane,
            FaceCameraPosition,
            LookAtPosition,
            LookAtLine,
            Advanced,
            FixedAxis,
            AlongVelocity,
        }

        public enum AxesPair
        {
            XY = 0,
            YZ = 1,
            ZX = 2,
            YX = 3,
            ZY = 4,
            XZ = 5,
        }

        [VFXSetting, Tooltip("Specifies the orientation mode of the particle. It can face towards the camera or a specific position, orient itself along the velocity or a fixed axis, or use more advanced facing behavior.")]
        public Mode mode;

        [VFXSetting, Tooltip("Specifies which two axes to use for the particle orientation.")]
        public AxesPair axes = AxesPair.ZY;
        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (mode != Mode.Advanced)
                {
                    yield return "axes";
                }
            }
        }

        public override string name { get { return "Orient : " + ObjectNames.NicifyVariableName(mode.ToString()); } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.Output; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);
                if (mode != Mode.Advanced && mode != Mode.FaceCameraPlane)
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (mode == Mode.AlongVelocity)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.LookAtPosition:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Position), "Position"));
                        break;

                    case Mode.LookAtLine:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Line), "Line"), Line.defaultValue);
                        break;

                    case Mode.Advanced:
                    {
                        string axis1, axis2;
                        Vector3 vector1, vector2;
                        AxesPairToUI(axes, out axis1, out axis2);
                        AxesPairToVector(axes, out vector1, out vector2);
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), axis1), new DirectionType() { direction = vector1 });
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), axis2), new DirectionType() { direction = vector2 });
                        break;
                    }

                    case Mode.FixedAxis:
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(DirectionType), "Up"), new DirectionType() { direction = Vector3.up });
                        break;
                }
            }
        }

        public override string source
        {
            get
            {
                switch (mode)
                {
                    case Mode.FaceCameraPlane:
                        return @"
float3x3 viewRot = GetVFXToViewRotMatrix();
axisX = viewRot[0].xyz;
axisY = viewRot[1].xyz;
axisZ = -viewRot[2].xyz;
#if VFX_LOCAL_SPACE // Need to remove potential scale in local transform
axisX = normalize(axisX);
axisY = normalize(axisY);
axisZ = normalize(axisZ);
#endif
";

                    case Mode.FaceCameraPosition:
                        return @"
if (unity_OrthoParams.w == 1.0f) // Face plane for ortho
{
    float3x3 viewRot = GetVFXToViewRotMatrix();
    axisX = viewRot[0].xyz;
    axisY = viewRot[1].xyz;
    axisZ = -viewRot[2].xyz;
    #if VFX_LOCAL_SPACE // Need to remove potential scale in local transform
    axisX = normalize(axisX);
    axisY = normalize(axisY);
    axisZ = normalize(axisZ);
    #endif
}
else
{
    axisZ = normalize(position - GetViewVFXPosition());
    axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
    axisY = cross(axisZ,axisX);
}
";

                    case Mode.LookAtPosition:
                        return @"
axisZ = normalize(position - Position);
axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.LookAtLine:
                        return @"
float3 lineDir = normalize(Line_end - Line_start);
float3 target = dot(position - Line_start,lineDir) * lineDir + Line_start;
axisZ = normalize(position - target);
axisX = normalize(cross(GetVFXToViewRotMatrix()[1].xyz,axisZ));
axisY = cross(axisZ,axisX);
";

                    case Mode.Advanced:
                    {
                        string rotAxis1, rotAxis2, rotAxis3, uiAxis1, uiAxis2;
                        AxesPairToHLSL(axes, out rotAxis1, out rotAxis2, out rotAxis3);
                        AxesPairToUI(axes, out uiAxis1, out uiAxis2);
                        string code = string.Format(@"
{0} = normalize({3});
{2} = normalize({4});
{1} = {5};
", rotAxis1, rotAxis2, rotAxis3,
                            uiAxis1, LeftHandedBasis(axes, uiAxis1, uiAxis2), LeftHandedBasis(GetSecondAxesPair(axes), rotAxis1, rotAxis3));
                        return code;
                    }

                    case Mode.FixedAxis:
                        return @"
axisY = Up;
axisZ = position - GetViewVFXPosition();
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);
";

                    case Mode.AlongVelocity:
                        return @"
axisY = normalize(velocity);
axisZ = position - GetViewVFXPosition();
axisX = normalize(cross(axisY,axisZ));
axisZ = cross(axisX,axisY);
";

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override void Sanitize(int version)
        {
            if (mode == Mode.LookAtPosition)
            {
                /* Slot of type position has changed from undefined VFXSlot to VFXSlotPosition*/
                if (GetNbInputSlots() > 0 && !(GetInputSlot(0) is VFXSlotPosition))
                {
                    var oldValue = GetInputSlot(0).value;
                    RemoveSlot(GetInputSlot(0));
                    AddSlot(VFXSlot.Create(new VFXProperty(typeof(Position), "Position"), VFXSlot.Direction.kInput, oldValue));
                }
            }
            base.Sanitize(version);
        }

        private void AxesPairToHLSL(AxesPair axes, out string axis1, out string axis2, out string axis3)
        {
            const string X = "axisX";
            const string Y = "axisY";
            const string Z = "axisZ";
            switch (axes)
            {
                case AxesPair.XY:
                    axis1 = X;
                    axis2 = Y;
                    axis3 = Z;
                    break;
                case AxesPair.XZ:
                    axis1 = X;
                    axis2 = Z;
                    axis3 = Y;
                    break;
                case AxesPair.YX:
                    axis1 = Y;
                    axis2 = X;
                    axis3 = Z;
                    break;
                case AxesPair.YZ:
                    axis1 = Y;
                    axis2 = Z;
                    axis3 = X;
                    break;
                case AxesPair.ZX:
                    axis1 = Z;
                    axis2 = X;
                    axis3 = Y;
                    break;
                case AxesPair.ZY:
                    axis1 = Z;
                    axis2 = Y;
                    axis3 = X;
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported axes pair");
            }
        }

        private void AxesPairToUI(AxesPair pair, out string uiAxis1, out string uiAxis2)
        {
            string axis1, axis2, axis3;
            AxesPairToHLSL(pair, out axis1, out axis2, out axis3);
            uiAxis1 = "Axis" + axis1[axis1.Length - 1];
            uiAxis2 = "Axis" + axis2[axis2.Length - 1];
        }

        private void AxesPairToVector(AxesPair pair, out Vector3 axis1, out Vector3 axis2)
        {
            Vector3 X = Vector3.right, Y = Vector3.up, Z = Vector3.forward;
            switch (pair)
            {
                case AxesPair.XY:
                    axis1 = X;
                    axis2 = Y;
                    break;
                case AxesPair.XZ:
                    axis1 = X;
                    axis2 = Z;
                    break;
                case AxesPair.YX:
                    axis1 = Y;
                    axis2 = X;
                    break;
                case AxesPair.YZ:
                    axis1 = Y;
                    axis2 = Z;
                    break;
                case AxesPair.ZX:
                    axis1 = Z;
                    axis2 = X;
                    break;
                case AxesPair.ZY:
                    axis1 = Z;
                    axis2 = Y;
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported axes pair");
            }
        }

        /// <summary>
        /// Given two axes in (X, Y, Z), compute the third one so that the resulting basis is left handed
        /// </summary>
        /// <param name="axis1">hlsl value of first axis in pair</param>
        /// <param name="axis2">hlsl value of second axis</param>
        private string LeftHandedBasis(AxesPair axes, string axis1, string axis2)
        {
            if (axes <= AxesPair.ZX)
                return "cross(" + axis1 + ", " + axis2 + ")";
            else
                return "cross(" + axis2 + ", " + axis1 + ")";
        }

        private AxesPair GetSecondAxesPair(AxesPair axes)
        {
            switch (axes)
            {
                case AxesPair.XY:
                    return AxesPair.XZ;
                case AxesPair.YZ:
                    return AxesPair.YX;
                case AxesPair.ZX:
                    return AxesPair.ZY;
                case AxesPair.YX:
                    return AxesPair.YZ;
                case AxesPair.ZY:
                    return AxesPair.ZX;
                case AxesPair.XZ:
                    return AxesPair.XY;
                default:
                    throw new InvalidEnumArgumentException("Unsupported axes pair");
            }
        }
    }
}
