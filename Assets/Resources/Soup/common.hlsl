#pragma once
static float fr = 0.7;
static float rad = 1.2;
static float mass = 0.05;
static float pi = 3.1415926;
static float gravity = -9.8;

float dt;
uint count;
float4 bounds;


float k;
float p0;

RWStructuredBuffer<float4> positions;
RWStructuredBuffer<float4> velocities;
RWStructuredBuffer<float> densities;

RWStructuredBuffer<float4x4> transforms;

float4x4 translate(float3 o) {
    return float4x4(
        1, 0, 0, o.x, 
        0, 1, 0, o.y, 
        0, 0, 1, o.z, 
        0, 0, 0, 1
    );
}

bool IsNaN(float x) {
    return !(x < 0.f || x > 0.f || x == 0.f);
}

void CheckBounds(uint id) {

    // branchless AABB check
    float2 max_bounds = bounds.xy + bounds.zw;

    float sign_x = sign((positions[id].x - bounds.x) * (positions[id].x - max_bounds.x));
    float sign_y = sign((positions[id].y - bounds.y) * (positions[id].y - max_bounds.y));

    //float2 signs = sign((positions[id].xy - bounds.xy) * (positions[id].xy * max_bounds));

    positions[id].x = clamp(positions[id].x, bounds.x, max_bounds.x);
    positions[id].y = clamp(positions[id].y, bounds.y, max_bounds.y);

    //positions[id].xy = clamp(positions[id].xy, bounds.xy, max_bounds);

    velocities[id].x *= 1 - (1 + fr) * max(0, sign_x);
    velocities[id].y *= 1 - (1 + fr) * max(0, sign_y);

    // velocities[id].xy = velocities[id].xy * (1 - (1 + fr) * max(0, signs));

}

float SmoothingKernel(float dist, float radius) {
    float den = 64 * pi * pow(abs(radius), 9);
    float c = 315 / den;

    //TODO: Piecewise branchless
    float rterm = 0;
    if (dist <= radius) {
        rterm = pow(radius * radius - dist * dist, 3);
    }
    
    return c * rterm;
}

float2 SmoothingKernelDeriv(float2 dist, float radius) {
    float2 r2 = float2(radius * radius, radius * radius);
    float2 n2 = r2 - (dist * dist);
    float2 n3 = n2 * n2;
    float2 num = float2(1, 1) * 945 * dist;

    if (length(dist) < radius) {
        num *= n3;
    } else {
        num *= 0;
    }
    num /= 32 * pow(abs(radius), 9) * pi;

    return -num;
}

float2 GetPressureForces(float2 position, float density, int id) {
    float2 force = float2(0, 0);
    float p1 = k * (density - p0);

    for (uint i = 0; i < count; i++) {
        if (int(i) == id) continue;

        float p2 = k * (densities[i] - p0);
        float dm = mass * (p1 - p2) / (2 * densities[i]);

        float2 dist = position - positions[i].xy;

        float2 pa = SmoothingKernelDeriv(dist, rad);
        force += p2 * dm * SmoothingKernelDeriv(dist, rad);
    }

    return -force;
}

void IntegrateForces(uint id) {
    float2 acc = float2(0, 0);

    float2 pos = positions[id].xy;
    float density = densities[id];
    acc += GetPressureForces(pos, density, id) / densities[id] * dt;

    velocities[id].xy += -acc * dt;
    positions[id] += velocities[id] * dt;  
}

float CalculateDensity(float2 position, int id) {
    float den = 0;
    for (uint i = 0; i < count; i++) {
        if (int(i) == id) continue;
        float2 rdist = position - positions[i].xy;

        float density = mass * SmoothingKernel(length(rdist), rad);

        den += density;
    }

    return den;
}