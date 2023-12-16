using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.GUI;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace DeepSpaceScanner
{
    public class Hud
    {
        Vector2 _textPositionPx = new Vector2(350, 183);
        Vector2 _charSizePX = new Vector2(18, 24);

        Vector2 _bgTextureLocation = new Vector2(5, 5);
        Vector2 _bgPositionPx = new Vector2(357, 183);
        Vector2 _bgSizePX = new Vector2(398, 117);

        Vector2 _iconTextureLocation = new Vector2(419, 25);
        Vector2 _iconPositionPx = new Vector2(303, 183);
        Vector2 _iconSizePX = new Vector2(79, 79);

        MyStringId _fontMaterial = MyStringId.GetOrCompute("FontData0");
        MyStringId _bgMaterial = MyStringId.GetOrCompute("SignatureTexture");
        Vector2 _bgRes = new Vector2(512, 128);

        float _viewportX;
        float _viewportY;
        float _fov;
        Vector2D _textPosition;
        Vector2D _bgPosition;
        Vector2D _iconPosition;
        Vector2D _charSize;
        float _charSpace;
        Vector2D _bgSize;
        Vector2D _iconSize;

        const float NEAR_PLANE_DISTANCE = 0.05f;
        readonly Vector4 BG_COLOR = new Vector4(0.159f, 0.21f, 0.244f, 1f);

        Dictionary<char, Vector2> _chars = new Dictionary<char, Vector2>
        {
            {0.ToString().First(), new Vector2(451, 11)},
            {1.ToString().First(), new Vector2(480, 11)},
            {2.ToString().First(), new Vector2(510, 11)},
            {3.ToString().First(), new Vector2(543, 11)},
            {4.ToString().First(), new Vector2(577, 11)},
            {5.ToString().First(), new Vector2(611, 11)},
            {6.ToString().First(), new Vector2(647, 11)},
            {7.ToString().First(), new Vector2(680, 11)},
            {8.ToString().First(), new Vector2(712, 11)},
            {9.ToString().First(), new Vector2(747, 11)},
        };

        void ScreenChanged()
        {
            _fov = MyAPIGateway.Session.Camera.FovWithZoom;
            _viewportX = MyAPIGateway.Session.Camera.ViewportSize.X;
            _viewportY = MyAPIGateway.Session.Camera.ViewportSize.Y;
            var ar = _viewportX / _viewportY;

            var height = Math.Tan(_fov / 2d) * 2 * NEAR_PLANE_DISTANCE;
            var width = height * ar;
            var vpPX = new Vector2D(_viewportX, _viewportY);
            var vp = new Vector2D(width, height);
            var screenScale = vp / vpPX;
            var vpHalf = vp / 2;
            var vpPXHalf = vpPX / 2;
            var scale = vpPX.X / 1920;

            var k = 2.38f;
            var mult = screenScale * scale / k;
            _charSize = _charSizePX.Vector2D() * mult;
            _bgSize = _bgSizePX.Vector2D() * mult;
            _iconSize = _iconSizePX.Vector2D() * mult;
            _textPosition = (_textPositionPx.Vector2D() * scale - vpPXHalf) / vpPXHalf * vpHalf;
            _bgPosition = (_bgPositionPx.Vector2D() * scale - vpPXHalf) / vpPXHalf * vpHalf;
            _iconPosition = (_iconPositionPx.Vector2D() * scale - vpPXHalf) / vpPXHalf * vpHalf;
            _charSpace = (float) screenScale.X * 2f / k;
        }

        public void Draw(uint signature, bool isScanned)
        {
            if (MyAPIGateway.Session.Config.MinimalHud || MyAPIGateway.Gui.IsCursorVisible) return;
            if (MyAPIGateway.Session?.Camera == null) return;
            if (MyAPIGateway.Session.Camera.FovWithZoom != _fov || _viewportX != MyAPIGateway.Session.Camera.ViewportSize.X ||
                _viewportY != MyAPIGateway.Session.Camera.ViewportSize.Y) ScreenChanged();

            DrawBackground();
            DrawIcon(isScanned);

            var normal = MyAPIGateway.Session.Camera.ViewMatrix.Forward;
            var matrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var left = (Vector3) matrix.Left;
            var up = (Vector3) matrix.Up;
            var i = 0d;

            foreach (var c in signature.ToString().PadLeft(5, '0'))
            {
                var @char = _chars[c];
                var offset = new Vector3D(_textPosition.X + i++ * (_charSize.X + _charSpace), _textPosition.Y, -NEAR_PLANE_DISTANCE);
                var charPos = Vector3D.Transform(offset, matrix);
                MyQuadD quad;
                MyUtils.GetBillboardQuadOriented(out quad, ref charPos, (float) _charSize.X / 2, (float) _charSize.Y / 2, ref left, ref up);

                var res = 1024;
                var v0 = @char / res;
                var v1 = new Vector2(@char.X + _charSizePX.X, @char.Y) / res;
                var v2 = new Vector2(@char.X, @char.Y + _charSizePX.Y) / res;
                var v3 = (@char + _charSizePX) / res;
                var color = isScanned ? Color.Red : Color.White;
                MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, normal, normal, normal, v0, v1, v3, _fontMaterial, 0xFFFFFFFF, charPos, color,
                    BlendTypeEnum.PostPP);
                MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, normal, normal, normal, v0, v2, v3, _fontMaterial, 0xFFFFFFFF, charPos, color,
                    BlendTypeEnum.PostPP);
            }
        }

        void DrawBackground()
        {
            var v0 = _bgTextureLocation / _bgRes;
            var v1 = new Vector2(_bgTextureLocation.X + _bgSizePX.X, _bgTextureLocation.Y) / _bgRes;
            var v2 = new Vector2(_bgTextureLocation.X, _bgTextureLocation.Y + _bgSizePX.Y) / _bgRes;
            var v3 = (_bgTextureLocation + _bgSizePX) / _bgRes;

            var normal = MyAPIGateway.Session.Camera.ViewMatrix.Forward;
            var matrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var left = (Vector3) matrix.Left;
            var up = (Vector3) matrix.Up;
            var pos = Vector3D.Transform(new Vector3D(_bgPosition, -NEAR_PLANE_DISTANCE), matrix);

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref pos, (float) _bgSize.X / 2, (float) _bgSize.Y / 2, ref left, ref up);
            var color = (BG_COLOR * MyAPIGateway.Session.Config.HUDBkOpacity).ToLinearRGB().PremultiplyColor();
            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, normal, normal, normal, v0, v1, v3, _bgMaterial, 0xFFFFFFFF, pos, color,
                BlendTypeEnum.PostPP);
            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, normal, normal, normal, v0, v2, v3, _bgMaterial, 0xFFFFFFFF, pos, color,
                BlendTypeEnum.PostPP);
        }

        void DrawIcon(bool isScanned)
        {
            var v0 = _iconTextureLocation / _bgRes;
            var v1 = new Vector2(_iconTextureLocation.X + _iconSizePX.X, _iconTextureLocation.Y) / _bgRes;
            var v2 = new Vector2(_iconTextureLocation.X, _iconTextureLocation.Y + _iconSizePX.Y) / _bgRes;
            var v3 = (_iconTextureLocation + _iconSizePX) / _bgRes;

            var normal = MyAPIGateway.Session.Camera.ViewMatrix.Forward;
            var matrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var left = (Vector3) matrix.Left;
            var up = (Vector3) matrix.Up;
            var pos = Vector3D.Transform(new Vector3D(_iconPosition, -NEAR_PLANE_DISTANCE), matrix);

            MyQuadD quad;
            MyUtils.GetBillboardQuadOriented(out quad, ref pos, (float) _iconSize.X / 2, (float) _iconSize.Y / 2, ref left, ref up);
            var color = isScanned ? Color.Red : new Color(186, 238, 249);
            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, normal, normal, normal, v0, v1, v3, _bgMaterial, 0xFFFFFFFF, pos, color,
                BlendTypeEnum.PostPP);
            MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, normal, normal, normal, v0, v2, v3, _bgMaterial, 0xFFFFFFFF, pos, color,
                BlendTypeEnum.PostPP);
        }
    }

    static class Extensions
    {
        public static Vector2D Vector2D(this Vector2 @this)
        {
            return new Vector2D(@this.X, @this.Y);
        }
        
        public static float ToLinearRGBComponent2(float c) => (double) c > 0.04045 ? (float) Math.Pow(((double) c + 0.055) / 1.055, 2.4) : c / 12.92f;
        public static Vector4 ToLinearRGB2(this Vector4 c) => new Vector4(ToLinearRGBComponent2(c.X), ToLinearRGBComponent2(c.Y), ToLinearRGBComponent2(c.Z), c.W);
    }
}