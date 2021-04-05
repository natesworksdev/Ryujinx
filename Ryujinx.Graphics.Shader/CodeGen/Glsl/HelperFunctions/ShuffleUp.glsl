float Helper_ShuffleUp(float x, uint index, uint mask, out bool valid)
{
    uint clamp = mask & 0x1fu;
    uint segMask = (mask >> 8) & 0x1fu;
    uint threadId = gl_SubGroupInvocationARB;
    uint minThreadId = threadId & segMask;
    uint srcThreadId = (threadId & 0x1fu) - index;
    valid = srcThreadId >= minThreadId;
    return valid ? readInvocationARB(x, (threadId & 32u) | srcThreadId) : x;
}