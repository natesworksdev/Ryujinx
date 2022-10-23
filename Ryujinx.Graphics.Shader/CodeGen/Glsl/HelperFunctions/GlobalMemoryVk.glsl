layout (buffer_reference, std430, buffer_reference_align = 8) buffer buffer_regions_block
{
    uint blockIndices[1 << 14];
    uvec2 pointers[];
};

layout (buffer_reference, std430, buffer_reference_align = 1) buffer uint8_t_ptr
{
    uint8_t value;
};

layout (buffer_reference, std430, buffer_reference_align = 2) buffer uint16_t_ptr
{
    uint16_t value;
};

layout (buffer_reference, std430, buffer_reference_align = 4) buffer uint_ptr
{
    uint value;
};

uvec2 Helper_TranslateAddress(uvec2 address)
{
    buffer_regions_block br = buffer_regions_block(s_page_table.xy);

    uint l0 = (address.x >> 12) & 0x3fff;
    uint l1 = ((address.x >> 26) & 0x3f) | ((address.y << 6) & 0x3fc0);

    uvec2 hostAddress = br.pointers[br.blockIndices[l1] + l0];

    hostAddress.x += (address.x & 0xfff);

    return hostAddress;
}