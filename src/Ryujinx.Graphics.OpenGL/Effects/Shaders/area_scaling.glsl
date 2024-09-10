#version 430 core
precision mediump float;
layout (local_size_x = 16, local_size_y = 16) in;
layout(rgba8, binding = 0, location=0) uniform image2D imgOutput;
layout( location=1 ) uniform sampler2D Source;
layout( location=2 ) uniform float srcX0;
layout( location=3 ) uniform float srcX1;
layout( location=4 ) uniform float srcY0;
layout( location=5 ) uniform float srcY1;
layout( location=6 ) uniform float dstX0;
layout( location=7 ) uniform float dstX1;
layout( location=8 ) uniform float dstY0;
layout( location=9 ) uniform float dstY1;
layout( location=10 ) uniform float scaleX;
layout( location=11 ) uniform float scaleY;

vec2 GetResolution()
{
    return vec2(abs(srcX1 - srcX0), abs(srcY1 - srcY0));
}

vec2 GetInvResolution()
{
    return vec2(1.0) / GetResolution();
}

vec2 GetWindowResolution()
{
    return vec2(abs(dstX1 - dstX0), abs(dstY1 - dstY0));
}

vec2 GetInvWindowResolution()
{
    return vec2(1.0) / GetWindowResolution();
}

/***** COLOR SAMPLING *****/

// Non filtered sample (nearest neighbor)
vec4 QuickSample(vec2 uv)
{
#if 0 // Test sampling range
	const float threshold = 0.00000001;
    vec2 xy = uv.xy * GetResolution();
	// Sampling outside the valid range, draw in yellow
	if (xy.x < (srcX0 - threshold) || xy.x > (srcX1 + threshold) || xy.y < (srcY0 - threshold) || xy.y > (srcY1 + threshold))
		return vec4(1.0, 1.0, 0.0, 1);
	// Sampling at the edges, draw in purple
	if (xy.x < srcX0 + 1.0 || xy.x > (srcX1 - 1.0) || xy.y < srcY0 + 1.0 || xy.y > (srcY1 - 1.0))
		return vec4(0.5, 0, 0.5, 1);
#endif
	return texture(Source, uv);
}
vec4 QuickSampleByPixel(vec2 xy)
{
	vec2 uv = vec2(xy * GetInvResolution());
	return QuickSample(uv);
}

/***** Area Sampling *****/

// By Sam Belliveau and Filippo Tarpini. Public Domain license.
// Effectively a more accurate sharp bilinear filter when upscaling,
// that also works as a mathematically perfect downscale filter.
// https://entropymine.com/imageworsener/pixelmixing/
// https://github.com/obsproject/obs-studio/pull/1715
// https://legacy.imagemagick.org/Usage/filter/
vec4 AreaSampling(vec2 xy)
{
	// Determine the sizes of the source and target images.
    vec2 source_size = GetResolution();
    vec2 target_size = GetWindowResolution();
    vec2 inverted_target_size = GetInvWindowResolution();

	// Compute the top-left and bottom-right corners of the target pixel box.
    vec2 t_beg = floor(xy - vec2(dstX0 < dstX1 ? dstX0 : dstX1, dstY0 < dstY1 ? dstY0 : dstY1));
    vec2 t_end = t_beg + vec2(1.0, 1.0);

	// Convert the target pixel box to source pixel box.
    vec2 beg = t_beg * inverted_target_size * source_size;
    vec2 end = t_end * inverted_target_size * source_size;

	// Compute the top-left and bottom-right corners of the pixel box.
    vec2 f_beg = floor(beg);
    vec2 f_end = floor(end);

	// Compute how much of the start and end pixels are covered horizontally & vertically.
	float area_w = 1.0 - fract(beg.x);
	float area_n = 1.0 - fract(beg.y);
	float area_e = fract(end.x);
	float area_s = fract(end.y);

	// Compute the areas of the corner pixels in the pixel box.
	float area_nw = area_n * area_w;
	float area_ne = area_n * area_e;
	float area_sw = area_s * area_w;
	float area_se = area_s * area_e;

	// Initialize the color accumulator.
	vec4 avg_color = vec4(0.0, 0.0, 0.0, 0.0);

	// Prevents rounding errors due to the coordinates flooring above
	const vec2 offset = vec2(0.5, 0.5);

	// Accumulate corner pixels.
	avg_color += area_nw * QuickSampleByPixel(vec2(f_beg.x, f_beg.y) + offset);
	avg_color += area_ne * QuickSampleByPixel(vec2(f_end.x, f_beg.y) + offset);
	avg_color += area_sw * QuickSampleByPixel(vec2(f_beg.x, f_end.y) + offset);
	avg_color += area_se * QuickSampleByPixel(vec2(f_end.x, f_end.y) + offset);

	// Determine the size of the pixel box.
	int x_range = int(f_end.x - f_beg.x - 0.5);
	int y_range = int(f_end.y - f_beg.y - 0.5);

	// Accumulate top and bottom edge pixels.
	for (int ix = 0; ix < x_range; ++ix)
	{
		float x = f_beg.x + 1.0 + float(ix);
		avg_color += area_n * QuickSampleByPixel(vec2(x, f_beg.y) + offset);
		avg_color += area_s * QuickSampleByPixel(vec2(x, f_end.y) + offset);
	}

	// Accumulate left and right edge pixels and all the pixels in between.
	for (int iy = 0; iy < y_range; ++iy)
	{
		float y = f_beg.y + 1.0 + float(iy);

		avg_color += area_w * QuickSampleByPixel(vec2(f_beg.x, y) + offset);
		avg_color += area_e * QuickSampleByPixel(vec2(f_end.x, y) + offset);

		for (int ix = 0; ix < x_range; ++ix)
		{
			float x = f_beg.x + 1.0 + float(ix);
			avg_color += QuickSampleByPixel(vec2(x, y) + offset);
		}
	}

	// Compute the area of the pixel box that was sampled.
	float area_corners = area_nw + area_ne + area_sw + area_se;
	float area_edges = float(x_range) * (area_n + area_s) + float(y_range) * (area_w + area_e);
	float area_center = float(x_range) * float(y_range);

	// Return the normalized average color.
	return avg_color / (area_corners + area_edges + area_center);
}

float insideBox(vec2 v, vec2 bLeft, vec2 tRight) {
    vec2 s = step(bLeft, v) - step(tRight, v);
    return s.x * s.y;
}

vec2 translateDest(vec2 pos) {
    vec2 translatedPos = vec2(pos.x, pos.y);
    translatedPos.x = dstX1 < dstX0 ? dstX1 - translatedPos.x : translatedPos.x;
    translatedPos.y = dstY0 > dstY1 ? dstY0 + dstY1 - translatedPos.y - 1 : translatedPos.y;
    return translatedPos;
}

void main()
{
    vec2 bLeft = vec2(dstX0 < dstX1 ? dstX0 : dstX1, dstY0 < dstY1 ? dstY0 : dstY1);
    vec2 tRight = vec2(dstX1 > dstX0 ? dstX1 : dstX0, dstY1 > dstY0 ? dstY1 : dstY0);
    ivec2 loc = ivec2(gl_GlobalInvocationID.x, gl_GlobalInvocationID.y);
    if (insideBox(loc, bLeft, tRight) == 0) {
        imageStore(imgOutput, loc, vec4(0, 0, 0, 1));
        return;
    }

    vec4 outColor = AreaSampling(loc);
    imageStore(imgOutput, ivec2(translateDest(loc)), outColor);
}
