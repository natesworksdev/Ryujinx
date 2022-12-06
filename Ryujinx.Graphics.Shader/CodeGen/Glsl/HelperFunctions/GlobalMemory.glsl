struct PTType
{
    uint blockIndices[1 << 14];
    uvec2 pointers[1 << 28];
};

uvec2 Helper_TranslateAddress(uvec2 address)
{
    PTType* br = (PTType*)packPtr(s_page_table.xy);

    uint l0 = (address.x >> 12) & 0x3fff;
    uint l1 = ((address.x >> 26) & 0x3f) | ((address.y << 6) & 0x3fc0);

    uvec2 hostAddress = br->pointers[br->blockIndices[l1] + l0];

    hostAddress.x += (address.x & 0xfff);

    return hostAddress;
}