using ARMeilleure.State;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    static class SvcTable
    {
        private const int SvcFuncMaxArguments64 = 8;
        private const int SvcFuncMaxArguments32 = 4;

        private static Dictionary<int, string> _svcFuncs64;
        private static Dictionary<int, string> _svcFuncs32;

        private static Action<SvcHandler, ExecutionContext>[] _svcTable64;
        private static Action<SvcHandler, ExecutionContext>[] _svcTable32;

        static SvcTable()
        {
            _svcFuncs64 = new Dictionary<int, string>
            {
                { 0x01, nameof(SvcHandler.SetHeapSize64)                   },
                { 0x03, nameof(SvcHandler.SetMemoryAttribute64)            },
                { 0x04, nameof(SvcHandler.MapMemory64)                     },
                { 0x05, nameof(SvcHandler.UnmapMemory64)                   },
                { 0x06, nameof(SvcHandler.QueryMemory64)                   },
                { 0x07, nameof(SvcHandler.ExitProcess64)                   },
                { 0x08, nameof(SvcHandler.CreateThread64)                  },
                { 0x09, nameof(SvcHandler.StartThread64)                   },
                { 0x0a, nameof(SvcHandler.ExitThread64)                    },
                { 0x0b, nameof(SvcHandler.SleepThread64)                   },
                { 0x0c, nameof(SvcHandler.GetThreadPriority64)             },
                { 0x0d, nameof(SvcHandler.SetThreadPriority64)             },
                { 0x0e, nameof(SvcHandler.GetThreadCoreMask64)             },
                { 0x0f, nameof(SvcHandler.SetThreadCoreMask64)             },
                { 0x10, nameof(SvcHandler.GetCurrentProcessorNumber64)     },
                { 0x11, nameof(SvcHandler.SignalEvent64)                   },
                { 0x12, nameof(SvcHandler.ClearEvent64)                    },
                { 0x13, nameof(SvcHandler.MapSharedMemory64)               },
                { 0x14, nameof(SvcHandler.UnmapSharedMemory64)             },
                { 0x15, nameof(SvcHandler.CreateTransferMemory64)          },
                { 0x16, nameof(SvcHandler.CloseHandle64)                   },
                { 0x17, nameof(SvcHandler.ResetSignal64)                   },
                { 0x18, nameof(SvcHandler.WaitSynchronization64)           },
                { 0x19, nameof(SvcHandler.CancelSynchronization64)         },
                { 0x1a, nameof(SvcHandler.ArbitrateLock64)                 },
                { 0x1b, nameof(SvcHandler.ArbitrateUnlock64)               },
                { 0x1c, nameof(SvcHandler.WaitProcessWideKeyAtomic64)      },
                { 0x1d, nameof(SvcHandler.SignalProcessWideKey64)          },
                { 0x1e, nameof(SvcHandler.GetSystemTick64)                 },
                { 0x1f, nameof(SvcHandler.ConnectToNamedPort64)            },
                { 0x21, nameof(SvcHandler.SendSyncRequest64)               },
                { 0x22, nameof(SvcHandler.SendSyncRequestWithUserBuffer64) },
                { 0x24, nameof(SvcHandler.GetProcessId64)                  },
                { 0x25, nameof(SvcHandler.GetThreadId64)                   },
                { 0x26, nameof(SvcHandler.Break64)                         },
                { 0x27, nameof(SvcHandler.OutputDebugString64)             },
                { 0x29, nameof(SvcHandler.GetInfo64)                       },
                { 0x2c, nameof(SvcHandler.MapPhysicalMemory64)             },
                { 0x2d, nameof(SvcHandler.UnmapPhysicalMemory64)           },
                { 0x32, nameof(SvcHandler.SetThreadActivity64)             },
                { 0x33, nameof(SvcHandler.GetThreadContext364)             },
                { 0x34, nameof(SvcHandler.WaitForAddress64)                },
                { 0x35, nameof(SvcHandler.SignalToAddress64)               },
                { 0x40, nameof(SvcHandler.CreateSession64)                 },
                { 0x41, nameof(SvcHandler.AcceptSession64)                 },
                { 0x43, nameof(SvcHandler.ReplyAndReceive64)               },
                { 0x45, nameof(SvcHandler.CreateEvent64)                   },
                { 0x65, nameof(SvcHandler.GetProcessList64)                },
                { 0x6f, nameof(SvcHandler.GetSystemInfo64)                 },
                { 0x70, nameof(SvcHandler.CreatePort64)                    },
                { 0x71, nameof(SvcHandler.ManageNamedPort64)               },
                { 0x72, nameof(SvcHandler.ConnectToPort64)                 },
                { 0x73, nameof(SvcHandler.SetProcessMemoryPermission64)    },
                { 0x77, nameof(SvcHandler.MapProcessCodeMemory64)          },
                { 0x78, nameof(SvcHandler.UnmapProcessCodeMemory64)        },
                { 0x7B, nameof(SvcHandler.TerminateProcess64)              }
            };

            _svcFuncs32 = new Dictionary<int, string>
            {
                { 0x01, nameof(SvcHandler.SetHeapSize32)                   },
                { 0x03, nameof(SvcHandler.SetMemoryAttribute32)            },
                { 0x04, nameof(SvcHandler.MapMemory32)                     },
                { 0x05, nameof(SvcHandler.UnmapMemory32)                   },
                { 0x06, nameof(SvcHandler.QueryMemory32)                   },
                { 0x07, nameof(SvcHandler.ExitProcess32)                   },
                { 0x08, nameof(SvcHandler.CreateThread32)                  },
                { 0x09, nameof(SvcHandler.StartThread32)                   },
                { 0x0a, nameof(SvcHandler.ExitThread32)                    },
                { 0x0b, nameof(SvcHandler.SleepThread32)                   },
                { 0x0c, nameof(SvcHandler.GetThreadPriority32)             },
                { 0x0d, nameof(SvcHandler.SetThreadPriority32)             },
                { 0x0f, nameof(SvcHandler.SetThreadCoreMask32)             },
                { 0x10, nameof(SvcHandler.GetCurrentProcessorNumber32)     },
                { 0x13, nameof(SvcHandler.MapSharedMemory32)               },
                { 0x14, nameof(SvcHandler.UnmapSharedMemory32)             },
                { 0x15, nameof(SvcHandler.CreateTransferMemory32)          },
                { 0x16, nameof(SvcHandler.CloseHandle32)                   },
                { 0x18, nameof(SvcHandler.WaitSynchronization32)           },
                { 0x1a, nameof(SvcHandler.ArbitrateLock32)                 },
                { 0x1b, nameof(SvcHandler.ArbitrateUnlock32)               },
                { 0x1c, nameof(SvcHandler.WaitProcessWideKeyAtomic32)      },
                { 0x1d, nameof(SvcHandler.SignalProcessWideKey32)          },
                { 0x1f, nameof(SvcHandler.ConnectToNamedPort32)            },
                { 0x21, nameof(SvcHandler.SendSyncRequest32)               },
                { 0x25, nameof(SvcHandler.GetThreadId32)                   },
                { 0x26, nameof(SvcHandler.Break32)                         },
                { 0x27, nameof(SvcHandler.OutputDebugString32)             },
                { 0x29, nameof(SvcHandler.GetInfo32)                       },
                { 0x34, nameof(SvcHandler.WaitForAddress32)                },
                { 0x35, nameof(SvcHandler.SignalToAddress32)               },

                { 0x5F, nameof(SvcHandler.FlushProcessDataCache32)         }
            };

            _svcTable64 = new Action<SvcHandler, ExecutionContext>[0x80];
            _svcTable32 = new Action<SvcHandler, ExecutionContext>[0x80];
        }

        public static Action<SvcHandler, ExecutionContext> GetSvcFunc(int svcId, bool aarch32)
        {
            Action<SvcHandler, ExecutionContext>[] table = aarch32 ? _svcTable32 : _svcTable64;
            if (table[svcId] != null)
            {
                return table[svcId];
            }

            Dictionary<int, string> funcTable = aarch32 ? _svcFuncs32 : _svcFuncs64;
            if (funcTable.TryGetValue(svcId, out string svcName))
            {
                Action<SvcHandler, ExecutionContext> svcFunc;

                if (aarch32)
                {
                    svcFunc = GenerateMethod32(svcName);
                }
                else
                {
                    svcFunc = GenerateMethod64(svcName);
                }
                table[svcId] = svcFunc;

                return table[svcId] = svcFunc;
            }

            if (aarch32 && _svcFuncs64.TryGetValue(svcId, out string svcName64))
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"Svc \"{svcName64}\" ({svcId}) does not have a 32-bit call signature defined - fell back to 64-bit.");
                throw new NotImplementedException($"Svc \"{svcName64}\" ({svcId}) does not have a 32-bit call signature defined.");
            }

            return null;
        }

        private static Action<SvcHandler, ExecutionContext> GenerateMethod64(string svcName)
        {
            Type[] argTypes = new Type[] { typeof(SvcHandler), typeof(ExecutionContext) };

            DynamicMethod method = new DynamicMethod(svcName, null, argTypes);

            MethodInfo methodInfo = typeof(SvcHandler).GetMethod(svcName);

            ParameterInfo[] methodArgs = methodInfo.GetParameters();

            if (methodArgs.Length > SvcFuncMaxArguments64)
            {
                throw new InvalidOperationException($"Method \"{svcName}\" has too many arguments, max is {SvcFuncMaxArguments64}.");
            }

            ILGenerator generator = method.GetILGenerator();

            void ConvertToArgType(Type sourceType)
            {
                CheckIfTypeIsSupported(sourceType, svcName);

                switch (Type.GetTypeCode(sourceType))
                {
                    case TypeCode.UInt32: generator.Emit(OpCodes.Conv_U4); break;
                    case TypeCode.Int32:  generator.Emit(OpCodes.Conv_I4); break;
                    case TypeCode.UInt16: generator.Emit(OpCodes.Conv_U2); break;
                    case TypeCode.Int16:  generator.Emit(OpCodes.Conv_I2); break;
                    case TypeCode.Byte:   generator.Emit(OpCodes.Conv_U1); break;
                    case TypeCode.SByte:  generator.Emit(OpCodes.Conv_I1); break;

                    case TypeCode.Boolean:
                        generator.Emit(OpCodes.Conv_I4);
                        generator.Emit(OpCodes.Ldc_I4_1);
                        generator.Emit(OpCodes.And);
                        break;
                }
            }

            void ConvertToFieldType(Type sourceType)
            {
                CheckIfTypeIsSupported(sourceType, svcName);

                switch (Type.GetTypeCode(sourceType))
                {
                    case TypeCode.UInt32:
                    case TypeCode.Int32:
                    case TypeCode.UInt16:
                    case TypeCode.Int16:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Boolean:
                        generator.Emit(OpCodes.Conv_U8);
                        break;
                }
            }

            // For functions returning output values, the first registers
            // are used to hold pointers where the value will be stored,
            // so they can't be used to pass argument and we must
            // skip them.
            int byRefArgsCount = 0;

            for (int index = 0; index < methodArgs.Length; index++)
            {
                if (methodArgs[index].ParameterType.IsByRef)
                {
                    byRefArgsCount++;
                }
            }

            BindingFlags staticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

            // Print all the arguments for debugging purposes.
            int inputArgsCount = methodArgs.Length - byRefArgsCount;

            if (inputArgsCount != 0)
            {
                generator.Emit(OpCodes.Ldc_I4, inputArgsCount);

                generator.Emit(OpCodes.Newarr, typeof(object));

                string argsFormat = svcName;

                for (int index = 0; index < inputArgsCount; index++)
                {
                    argsFormat += $" {methodArgs[index].Name}: 0x{{{index}:X8}},";

                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldc_I4, index);

                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldc_I4, byRefArgsCount + index);

                    MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.GetX));

                    generator.Emit(OpCodes.Call, info);

                    generator.Emit(OpCodes.Box, typeof(ulong));

                    generator.Emit(OpCodes.Stelem_Ref);
                }

                argsFormat = argsFormat.Substring(0, argsFormat.Length - 1);

                generator.Emit(OpCodes.Ldstr, argsFormat);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);

                generator.Emit(OpCodes.Ldstr, svcName);
            }

            MethodInfo printArgsMethod = typeof(SvcTable).GetMethod(nameof(PrintArguments), staticNonPublic);

            generator.Emit(OpCodes.Call, printArgsMethod);

            // Call the SVC function handler.
            generator.Emit(OpCodes.Ldarg_0);

            List<LocalBuilder> locals = new List<LocalBuilder>();

            for (int index = 0; index < methodArgs.Length; index++)
            {
                Type argType = methodArgs[index].ParameterType;

                if (argType.IsByRef)
                {
                    argType = argType.GetElementType();

                    LocalBuilder local = generator.DeclareLocal(argType);

                    locals.Add(local);

                    if (!methodArgs[index].IsOut)
                    {
                        generator.Emit(OpCodes.Ldarg_1);
                        generator.Emit(OpCodes.Ldc_I4, index);

                        MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.GetX));

                        generator.Emit(OpCodes.Call, info);

                        ConvertToArgType(argType);

                        generator.Emit(OpCodes.Stloc, local);
                    }

                    generator.Emit(OpCodes.Ldloca, local);
                }
                else
                {
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldc_I4, byRefArgsCount + index);

                    MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.GetX));

                    generator.Emit(OpCodes.Call, info);

                    ConvertToArgType(argType);
                }
            }

            generator.Emit(OpCodes.Call, methodInfo);

            int outRegIndex = 0;

            Type retType = methodInfo.ReturnType;

            // Print result code.
            if (retType == typeof(KernelResult))
            {
                MethodInfo printResultMethod = typeof(SvcTable).GetMethod(nameof(PrintResult), staticNonPublic);

                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldstr, svcName);
                generator.Emit(OpCodes.Call, printResultMethod);
            }

            // Save return value into register X0 (when the method has a return value).
            if (retType != typeof(void))
            {
                CheckIfTypeIsSupported(retType, svcName);

                LocalBuilder tempLocal = generator.DeclareLocal(retType);

                generator.Emit(OpCodes.Stloc, tempLocal);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, outRegIndex++);
                generator.Emit(OpCodes.Ldloc, tempLocal);

                ConvertToFieldType(retType);

                MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.SetX));

                generator.Emit(OpCodes.Call, info);
            }

            for (int index = 0; index < locals.Count; index++)
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, outRegIndex++);
                generator.Emit(OpCodes.Ldloc, locals[index]);

                ConvertToFieldType(locals[index].LocalType);

                MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.SetX));

                generator.Emit(OpCodes.Call, info);
            }

            // Zero out the remaining unused registers.
            while (outRegIndex < SvcFuncMaxArguments64)
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, outRegIndex++);
                generator.Emit(OpCodes.Ldc_I8, 0L);

                MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.SetX));

                generator.Emit(OpCodes.Call, info);
            }

            generator.Emit(OpCodes.Ret);

            return (Action<SvcHandler, ExecutionContext>)method.CreateDelegate(typeof(Action<SvcHandler, ExecutionContext>));
        }

        // TODO: merge 64 bits variant
        private static Action<SvcHandler, ExecutionContext> GenerateMethod32(string svcName)
        {
            Type[] argTypes = new Type[] { typeof(SvcHandler), typeof(ExecutionContext) };

            DynamicMethod method = new DynamicMethod(svcName, null, argTypes);

            MethodInfo methodInfo = typeof(SvcHandler).GetMethod(svcName);

            ParameterInfo[] methodArgs = methodInfo.GetParameters();
            int numArgs = methodArgs.Count(x => !x.IsOut);

            ILGenerator generator = method.GetILGenerator();

            void ConvertToArgType(Type sourceType)
            {
                CheckIfTypeIsSupported(sourceType, svcName);

                switch (Type.GetTypeCode(sourceType))
                {
                    case TypeCode.UInt32: generator.Emit(OpCodes.Conv_U4); break;
                    case TypeCode.Int32:  generator.Emit(OpCodes.Conv_I4); break;
                    case TypeCode.UInt16: generator.Emit(OpCodes.Conv_U2); break;
                    case TypeCode.Int16:  generator.Emit(OpCodes.Conv_I2); break;
                    case TypeCode.Byte:   generator.Emit(OpCodes.Conv_U1); break;
                    case TypeCode.SByte:  generator.Emit(OpCodes.Conv_I1); break;

                    case TypeCode.Boolean:
                        generator.Emit(OpCodes.Conv_I4);
                        generator.Emit(OpCodes.Ldc_I4_1);
                        generator.Emit(OpCodes.And);
                        break;
                }
            }

            void ConvertToFieldType(Type sourceType)
            {
                CheckIfTypeIsSupported(sourceType, svcName);

                switch (Type.GetTypeCode(sourceType))
                {
                    case TypeCode.UInt32:
                    case TypeCode.Int32:
                    case TypeCode.UInt16:
                    case TypeCode.Int16:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Boolean:
                        generator.Emit(OpCodes.Conv_U8);
                        break;
                }
            }

            RAttribute GetRegisterAttribute(ParameterInfo parameterInfo)
            {
                RAttribute argumentAttribute = (RAttribute)parameterInfo.GetCustomAttribute(typeof(RAttribute));

                if (argumentAttribute == null)
                {
                    throw new InvalidOperationException($"Method \"{svcName}\" is missing a {typeof(RAttribute).Name} attribute on parameter \"{parameterInfo.Name}\"");
                }

                return argumentAttribute;
            }

            // For functions returning output values, the first registers
            // are used to hold pointers where the value will be stored,
            // so they can't be used to pass argument and we must
            // skip them.
            int byRefArgsCount = 0;

            for (int index = 0; index < methodArgs.Length; index++)
            {
                if (methodArgs[index].ParameterType.IsByRef)
                {
                    byRefArgsCount++;
                }
            }

            BindingFlags staticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;

            // Print all the arguments for debugging purposes.
            int inputArgsCount = methodArgs.Length - byRefArgsCount;

            if (inputArgsCount != 0)
            {
                generator.Emit(OpCodes.Ldc_I4, inputArgsCount);

                generator.Emit(OpCodes.Newarr, typeof(object));

                string argsFormat = svcName;

                for (int index = 0; index < methodArgs.Length; index++)
                {
                    Type argType = methodArgs[index].ParameterType;

                    // Ignore out argument for printing
                    if (argType.IsByRef)
                    {
                        continue;
                    }

                    RAttribute registerAttribute = GetRegisterAttribute(methodArgs[index]);

                    argsFormat += $" {methodArgs[index].Name}: 0x{{{index}:X8}},";

                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldc_I4, index);

                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldc_I4, registerAttribute.Index);

                    MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.GetX));

                    generator.Emit(OpCodes.Call, info);

                    generator.Emit(OpCodes.Box, typeof(ulong));

                    generator.Emit(OpCodes.Stelem_Ref);
                }

                argsFormat = argsFormat.Substring(0, argsFormat.Length - 1);

               generator.Emit(OpCodes.Ldstr, argsFormat);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);

                generator.Emit(OpCodes.Ldstr, svcName);
            }

            MethodInfo printArgsMethod = typeof(SvcTable).GetMethod(nameof(PrintArguments), staticNonPublic);

            generator.Emit(OpCodes.Call, printArgsMethod);

            // Call the SVC function handler.
            generator.Emit(OpCodes.Ldarg_0);

            List<(LocalBuilder, RAttribute)> locals = new List<(LocalBuilder, RAttribute)>();

            for (int index = 0; index < methodArgs.Length; index++)
            {
                Type argType = methodArgs[index].ParameterType;
                RAttribute registerAttribute = GetRegisterAttribute(methodArgs[index]);

                if (argType.IsByRef)
                {
                    argType = argType.GetElementType();

                    LocalBuilder local = generator.DeclareLocal(argType);

                    locals.Add((local, registerAttribute));

                    if (!methodArgs[index].IsOut)
                    {
                        generator.Emit(OpCodes.Ldarg_1);
                        generator.Emit(OpCodes.Ldc_I4, registerAttribute.Index);

                        MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.GetX));

                        generator.Emit(OpCodes.Call, info);

                        ConvertToArgType(argType);

                        generator.Emit(OpCodes.Stloc, local);
                    }

                    generator.Emit(OpCodes.Ldloca, local);
                }
                else
                {
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldc_I4, registerAttribute.Index);

                    MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.GetX));

                    generator.Emit(OpCodes.Call, info);

                    ConvertToArgType(argType);
                }
            }

            generator.Emit(OpCodes.Call, methodInfo);

            int outRegIndex = 0;

            Type retType = methodInfo.ReturnType;

            // Print result code.
            if (retType == typeof(KernelResult))
            {
                MethodInfo printResultMethod = typeof(SvcTable).GetMethod(nameof(PrintResult), staticNonPublic);

                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldstr, svcName);
                generator.Emit(OpCodes.Call, printResultMethod);
            }

            // Save return value into register X0 (when the method has a return value).
            if (retType != typeof(void))
            {
                CheckIfTypeIsSupported(retType, svcName);

                LocalBuilder tempLocal = generator.DeclareLocal(retType);

                generator.Emit(OpCodes.Stloc, tempLocal);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, outRegIndex++);
                generator.Emit(OpCodes.Ldloc, tempLocal);

                ConvertToFieldType(retType);

                MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.SetX));

                generator.Emit(OpCodes.Call, info);
            }

            for (int index = 0; index < locals.Count; index++)
            {
                (LocalBuilder local, RAttribute attribute) = locals[index];
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, attribute.Index);
                generator.Emit(OpCodes.Ldloc, local);

                ConvertToFieldType(local.LocalType);

                MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.SetX));

                generator.Emit(OpCodes.Call, info);
            }

            bool IsRegisterInUse(int registerIndex)
            {
                for (int index = 0; index < locals.Count; index++)
                {
                    if (registerIndex == locals[index].Item2.Index)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Zero out the remaining unused registers.
            for (int i = 0; i < SvcFuncMaxArguments32; i++)
            {
                if (IsRegisterInUse(i))
                {
                    continue;
                }

                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Ldc_I8, 0L);

                MethodInfo info = typeof(ExecutionContext).GetMethod(nameof(ExecutionContext.SetX));

                generator.Emit(OpCodes.Call, info);
            }

            generator.Emit(OpCodes.Ret);

            return (Action<SvcHandler, ExecutionContext>)method.CreateDelegate(typeof(Action<SvcHandler, ExecutionContext>));
        }

        private static void CheckIfTypeIsSupported(Type type, string svcName)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt64:
                case TypeCode.Int64:
                case TypeCode.UInt32:
                case TypeCode.Int32:
                case TypeCode.UInt16:
                case TypeCode.Int16:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Boolean:
                    return;
            }

            throw new InvalidSvcException($"Method \"{svcName}\" has a invalid ref type \"{type.Name}\".");
        }

        private static void PrintArguments(object[] argValues, string formatOrSvcName)
        {
            if (argValues != null)
            {
                Logger.PrintDebug(LogClass.KernelSvc, string.Format(formatOrSvcName, argValues));
            }
            else
            {
                Logger.PrintDebug(LogClass.KernelSvc, formatOrSvcName);
            }
        }

        private static void PrintResult(KernelResult result, string svcName)
        {
            if (result != KernelResult.Success   &&
                result != KernelResult.TimedOut  &&
                result != KernelResult.Cancelled &&
                result != KernelResult.InvalidState)
            {
                Logger.PrintWarning(LogClass.KernelSvc, $"{svcName} returned error {result}.");
            }
            else
            {
                Logger.PrintDebug(LogClass.KernelSvc, $"{svcName} returned result {result}.");
            }
        }
    }
}
