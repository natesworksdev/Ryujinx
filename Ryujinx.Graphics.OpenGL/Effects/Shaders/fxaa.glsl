// https://www.shadertoy.com/view/ls3GWS
#version 430 core
precision mediump float;

layout(local_size_x = 16, local_size_y = 16) in;
layout(rgba8, binding = 0) uniform image2D imgOutput;

uniform sampler2D inputTexture;
layout(location=0) uniform vec2 invResolution; // 1.0 / resolution


#define FXAA_SPAN_MAX 8.0
#define FXAA_REDUCE_MUL   (1.0/FXAA_SPAN_MAX)
#define FXAA_REDUCE_MIN   (1.0/128.0)
#define FXAA_SUBPIX_SHIFT (1.0/4.0)

vec3 FxaaPixelShader( vec4 uv, sampler2D tex, vec2 rcpFrame) {    
    vec3 rgbNW = textureLod(tex, uv.zw, 0.0).xyz;
    vec3 rgbNE = textureLod(tex, uv.zw + vec2(1,0)*rcpFrame.xy, 0.0).xyz;
    vec3 rgbSW = textureLod(tex, uv.zw + vec2(0,1)*rcpFrame.xy, 0.0).xyz;
    vec3 rgbSE = textureLod(tex, uv.zw + vec2(1,1)*rcpFrame.xy, 0.0).xyz;
    vec3 rgbM  = textureLod(tex, uv.xy, 0.0).xyz;

    vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);

    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float dirReduce = max(
        (lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL),
        FXAA_REDUCE_MIN);
    float rcpDirMin = 1.0/(min(abs(dir.x), abs(dir.y)) + dirReduce);
    
    dir = min(vec2( FXAA_SPAN_MAX,  FXAA_SPAN_MAX),
        max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
        dir * rcpDirMin)) * rcpFrame.xy;

    vec3 rgbA = (1.0/2.0) * (
        textureLod(tex, uv.xy + dir * (1.0/3.0 - 0.5), 0.0).xyz +
        textureLod(tex, uv.xy + dir * (2.0/3.0 - 0.5), 0.0).xyz);
    vec3 rgbB = rgbA * (1.0/2.0) + (1.0/4.0) * (
        textureLod(tex, uv.xy + dir * (0.0/3.0 - 0.5), 0.0).xyz +
        textureLod(tex, uv.xy + dir * (3.0/3.0 - 0.5), 0.0).xyz);
    
    float lumaB = dot(rgbB, luma);

    if((lumaB < lumaMin) || (lumaB > lumaMax)) return rgbA;

    return rgbB; 
}


vec4 mainImage(vec2 fragCoord)
{
    vec2 rcpFrame = 1./invResolution.xy;
  	vec2 uv2 = fragCoord.xy / invResolution.xy;

    vec3 col;

    vec4 uv = vec4( uv2, uv2 - (rcpFrame * (0.5 + FXAA_SUBPIX_SHIFT)));
    col = FxaaPixelShader( uv, inputTexture, rcpFrame );

    vec4 fragColor = vec4( col, 1. );

    return fragColor;
}

void main()
{
    ivec2 loc = ivec2(gl_GlobalInvocationID.x * 4, gl_GlobalInvocationID.y * 4);
    for(int i = 0; i < 4; i++)
    {
        for(int j = 0; j < 4; j++)
        {
            ivec2 texelCoord = ivec2(loc.x + i, loc.y + j);
            vec4 outColor = mainImage(texelCoord + vec2(0.5));
            imageStore(imgOutput, texelCoord, outColor);
        }
    }
}