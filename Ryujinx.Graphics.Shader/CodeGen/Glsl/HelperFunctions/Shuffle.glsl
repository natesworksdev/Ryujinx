float Helper_Shuffle(float x, uint index, uint mask, out bool valid)
{
    uint clamp = mask & 0x1fu;
    uint segMask = (mask >> 8) & 0x1fu;
    uint threadId = gl_SubGroupInvocationARB;
    uint minThreadId = threadId & segMask;
    uint maxThreadId = minThreadId | (clamp & ~segMask);
    uint srcThreadId = (index & ~segMask) | minThreadId;
    valid = srcThreadId <= maxThreadId;
    return valid ? readInvocationARB(x, (threadId & 32u) | srcThreadId) : x;
}