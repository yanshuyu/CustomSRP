#ifndef SRP_SHADER_LIBRARY_SURFACE
#define SRP_SHADER_LIBRARY_SURFACE

struct Surface {
    real3 color;
    real3 normal;
    real3 viewDirection;
    real alpha;
    real metallic;
    real smoothness;
};


#endif