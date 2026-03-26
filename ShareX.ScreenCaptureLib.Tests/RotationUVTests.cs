using System;
using System.Numerics;
using ShareX.ScreenCaptureLib.AdvancedGraphics.Direct3D;
using ShareX.ScreenCaptureLib.AdvancedGraphics.Direct3D.Shaders;
using Vortice.DXGI;
using Vortice.Mathematics;
using Xunit;

namespace ShareX.ScreenCaptureLib.Tests;

public class RotationUVTests
{
    private const float Tolerance = 1e-5f;

    private static void AssertUV(Vector2 expected, Vector2 actual, string label)
    {
        Assert.True(
            MathF.Abs(expected.X - actual.X) < Tolerance &&
            MathF.Abs(expected.Y - actual.Y) < Tolerance,
            $"{label}: expected ({expected.X:F4},{expected.Y:F4}) but got ({actual.X:F4},{actual.Y:F4})");
    }

    /// <summary>
    /// Helper: extract the top-left, top-right, bottom-left, bottom-right UV
    /// from the 6-vertex triangle list returned by BuildRotatedQuad.
    /// Vertex layout: TL, TR, BL, BL, TR, BR → indices 0,1,2,5
    /// </summary>
    private static (Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br) ExtractCornerUVs(Vertex[] verts)
    {
        Assert.Equal(6, verts.Length);
        return (verts[0].TextureCoord, verts[1].TextureCoord,
                verts[2].TextureCoord, verts[5].TextureCoord);
    }

    // ----------------------------------------------------------------
    // Identity – full monitor
    // ----------------------------------------------------------------
    [Fact]
    public void Identity_FullMonitor_UVsArePassthrough()
    {
        // 1920×1080 landscape, no rotation
        var srcBox = new Box { Left = 0, Top = 0, Right = 1920, Bottom = 1080, Front = 0, Back = 1 };
        var verts = Tonemapping.BuildRotatedQuad(srcBox, 1920, 1080, ModeRotation.Identity);
        var (tl, tr, bl, br) = ExtractCornerUVs(verts);

        AssertUV(new Vector2(0, 0), tl, "TL");
        AssertUV(new Vector2(1, 0), tr, "TR");
        AssertUV(new Vector2(0, 1), bl, "BL");
        AssertUV(new Vector2(1, 1), br, "BR");
    }

    // ----------------------------------------------------------------
    // Rotate90 – full monitor (portrait, 1080×1920 desktop on 1920×1080 native panel)
    // ----------------------------------------------------------------
    [Fact]
    public void Rotate90_FullMonitor_UVsAreCorrect()
    {
        // Desktop 1080×1920, native texture 1920×1080
        var srcBox = new Box { Left = 0, Top = 0, Right = 1080, Bottom = 1920, Front = 0, Back = 1 };
        var verts = Tonemapping.BuildRotatedQuad(srcBox, 1080, 1920, ModeRotation.Rotate90);
        var (tl, tr, bl, br) = ExtractCornerUVs(verts);

        // Rotate90 mapping: tu = dv, tv = 1 - du
        // TL (du=0, dv=0) → (0, 1)
        // TR (du=1, dv=0) → (0, 0)
        // BL (du=0, dv=1) → (1, 1)
        // BR (du=1, dv=1) → (1, 0)
        AssertUV(new Vector2(0, 1), tl, "TL");
        AssertUV(new Vector2(0, 0), tr, "TR");
        AssertUV(new Vector2(1, 1), bl, "BL");
        AssertUV(new Vector2(1, 0), br, "BR");
    }

    // ----------------------------------------------------------------
    // Rotate270 – full monitor (portrait CCW, 1080×1920 desktop on 1920×1080 native panel)
    // ----------------------------------------------------------------
    [Fact]
    public void Rotate270_FullMonitor_UVsAreCorrect()
    {
        var srcBox = new Box { Left = 0, Top = 0, Right = 1080, Bottom = 1920, Front = 0, Back = 1 };
        var verts = Tonemapping.BuildRotatedQuad(srcBox, 1080, 1920, ModeRotation.Rotate270);
        var (tl, tr, bl, br) = ExtractCornerUVs(verts);

        // Rotate270 mapping: tu = 1 - dv, tv = du
        // TL (du=0, dv=0) → (1, 0)
        // TR (du=1, dv=0) → (1, 1)
        // BL (du=0, dv=1) → (0, 0)
        // BR (du=1, dv=1) → (0, 1)
        AssertUV(new Vector2(1, 0), tl, "TL");
        AssertUV(new Vector2(1, 1), tr, "TR");
        AssertUV(new Vector2(0, 0), bl, "BL");
        AssertUV(new Vector2(0, 1), br, "BR");
    }

    // ----------------------------------------------------------------
    // Rotate180 – full monitor
    // ----------------------------------------------------------------
    [Fact]
    public void Rotate180_FullMonitor_UVsAreCorrect()
    {
        var srcBox = new Box { Left = 0, Top = 0, Right = 1920, Bottom = 1080, Front = 0, Back = 1 };
        var verts = Tonemapping.BuildRotatedQuad(srcBox, 1920, 1080, ModeRotation.Rotate180);
        var (tl, tr, bl, br) = ExtractCornerUVs(verts);

        // Rotate180: tu = 1 - du, tv = 1 - dv
        AssertUV(new Vector2(1, 1), tl, "TL");
        AssertUV(new Vector2(0, 1), tr, "TR");
        AssertUV(new Vector2(1, 0), bl, "BL");
        AssertUV(new Vector2(0, 0), br, "BR");
    }

    // ----------------------------------------------------------------
    // Rotate90 – sub-region (partial capture on a portrait monitor)
    // ----------------------------------------------------------------
    [Fact]
    public void Rotate90_SubRegion_UVsAreCorrect()
    {
        // Desktop 1080×1920, capture the top-left quarter: 0,0 → 540,960
        var srcBox = new Box { Left = 0, Top = 0, Right = 540, Bottom = 960, Front = 0, Back = 1 };
        var verts = Tonemapping.BuildRotatedQuad(srcBox, 1080, 1920, ModeRotation.Rotate90);
        var (tl, tr, bl, br) = ExtractCornerUVs(verts);

        // du0=0, dv0=0, du1=0.5, dv1=0.5
        // TL (du=0,   dv=0)   → (0,   1)
        // TR (du=0.5, dv=0)   → (0,   0.5)
        // BL (du=0,   dv=0.5) → (0.5, 1)
        // BR (du=0.5, dv=0.5) → (0.5, 0.5)
        AssertUV(new Vector2(0f, 1f), tl, "TL");
        AssertUV(new Vector2(0f, 0.5f), tr, "TR");
        AssertUV(new Vector2(0.5f, 1f), bl, "BL");
        AssertUV(new Vector2(0.5f, 0.5f), br, "BR");
    }

    // ----------------------------------------------------------------
    // IsRotated helper
    // ----------------------------------------------------------------
    [Theory]
    [InlineData(ModeRotation.Identity, false)]
    [InlineData(ModeRotation.Unspecified, false)]
    [InlineData(ModeRotation.Rotate90, true)]
    [InlineData(ModeRotation.Rotate180, true)]
    [InlineData(ModeRotation.Rotate270, true)]
    public void IsRotated_ReturnsCorrectValue(ModeRotation rotation, bool expected)
    {
        Assert.Equal(expected, Tonemapping.IsRotated(rotation));
    }
}
