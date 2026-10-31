/* Thin dynamic ABI; Nuked-OPL3 remains an independently replaceable shared library. */
#include <stdlib.h>
#include "../third_party/nuked-opl3/opl3.h"
void *por_opl_create(void) { opl3_chip *p = calloc(1, sizeof(*p)); if(p) OPL3_Reset(p,48000); return p; }
void por_opl_destroy(void *p) { free(p); }
void por_opl_write(void *p, int reg, int value) { OPL3_WriteReg(p,(uint16_t)reg,(uint8_t)value); }
int por_opl_sample(void *p) { int16_t pair[2]; OPL3_GenerateResampled(p,pair); return ((int)pair[0]+pair[1])/2; }
