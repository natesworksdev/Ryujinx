layout (binding = 0, std430) buffer buffer_regions_block
{
    uvec4 buffer_regions[];
};

uvec2 Helper_TranslateAddress(uvec2 address)
{
    uint64_t address64 = packUint2x32(address);
    uint count = buffer_regions[0].x;
    uint left = 0;
    uint right = count;

    while (left != right)
    {
        uint middle = left + ((right - left) >> 1);
        uint offset = middle * 2;
        uvec4 guest_info = buffer_regions[1 + offset];
        uvec4 host_info = buffer_regions[2 + offset];

        uint64_t start_address = packUint2x32(guest_info.xy);
        uint64_t end_address = packUint2x32(guest_info.zw);
        if (address64 >= start_address && address64 < end_address)
        {
            uint64_t host_address = packUint2x32(host_info.xy);
            return unpackUint2x32((address64 - start_address) + host_address);
        }

        if (address64 < start_address)
        {
            right = middle;
        }
        else
        {
            left = middle + 1;
        }
    }

    return uvec2(0, 0);
}