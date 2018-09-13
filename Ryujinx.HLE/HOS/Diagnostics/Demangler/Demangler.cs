using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ryujinx.HLE.HOS.Diagnostics.Demangler.Ast;

namespace Ryujinx.HLE.HOS.Diagnostics.Demangler
{
    class Demangler
    {
        private static readonly string BASE_36 = "0123456789abcdefghijklmnopqrstuvwxyz";
        private List<BaseNode> SubstitutionList = new List<BaseNode>();
        private List<BaseNode> TemplateParamList = new List<BaseNode>();
        private List<ForwardTemplateReference> ForwardTemplateReferenceList = new List<ForwardTemplateReference>();

        public string Mangled { get; private set; }

        private int Pos;
        private int Length;

        private bool CanForwardTemplateReference;
        private bool CanParseTemplateArgs;
        public Demangler(string Mangled)
        {
            this.Mangled = Mangled;
            Pos = 0;
            Length = Mangled.Length;
            CanParseTemplateArgs = true;
        }

        private bool ConsumeIf(string ToConsume)
        {
            string MangledPart = Mangled.Substring(Pos);
            if (MangledPart.StartsWith(ToConsume))
            {
                Pos += ToConsume.Length;
                return true;
            }

            return false;
        }

        private string PeekString(int Offset = 0, int Length = 1)
        {
            if (Pos + Offset >= Length)
                return null;
            return Mangled.Substring(Pos + Offset, Length);
        }
        private char Peek(int Offset = 0)
        {
            if (Pos + Offset >= Length)
                return '\0';
            return Mangled[Pos + Offset];
        }

        private char Consume()
        {
            if (Pos < Length)
            {
                return Mangled[Pos++];
            }
            return '\0';
        }

        private int Count()
        {
            return Length - Pos;
        }

        private static int FromBase36(string encoded)
        {
            char[] reversedEncoded = encoded.ToLower().ToCharArray().Reverse().ToArray();
            int result = 0;
            for (int i = 0; i < reversedEncoded.Length; i++)
            {
                char c = reversedEncoded[i];
                int value = BASE_36.IndexOf(c);
                if (value == -1)
                    return -1;
                result += value * (int)Math.Pow(36, i);
            }
            return result;
        }

        private int ParseSeqId()
        {
            string Part = Mangled.Substring(Pos);

            int SeqIdLen;
            for (SeqIdLen = 0; SeqIdLen < Part.Length; SeqIdLen++)
            {
                if (!char.IsLetterOrDigit(Part[SeqIdLen]))
                    break;
            }
            Pos += SeqIdLen;
            return FromBase36(Part.Substring(0, SeqIdLen));
        }

        //   <substitution> ::= S <seq-id> _
        //                  ::= S_
        //                  ::= St # std::
        //                  ::= Sa # std::allocator
        //                  ::= Sb # std::basic_string
        //                  ::= Ss # std::basic_string<char, std::char_traits<char>, std::allocator<char> >
        //                  ::= Si # std::basic_istream<char, std::char_traits<char> >
        //                  ::= So # std::basic_ostream<char, std::char_traits<char> >
        //                  ::= Sd # std::basic_iostream<char, std::char_traits<char> >
        private BaseNode ParseSubstitution()
        {
            if (!ConsumeIf("S"))
                return null;
            char C = Peek();
            if (char.IsLower(C))
            {
                switch (C)
                {
                    case 'a':
                        Pos++;
                        return new SpecialSubstitution(SpecialSubstitution.SpecialType.Allocator);
                    case 'b':
                        Pos++;
                        return new SpecialSubstitution(SpecialSubstitution.SpecialType.BasicString);
                    case 's':
                        Pos++;
                        return new SpecialSubstitution(SpecialSubstitution.SpecialType.String);
                    case 'i':
                        Pos++;
                        return new SpecialSubstitution(SpecialSubstitution.SpecialType.IStream);
                    case 'o':
                        Pos++;
                        return new SpecialSubstitution(SpecialSubstitution.SpecialType.OStream);
                    case 'd':
                        Pos++;
                        return new SpecialSubstitution(SpecialSubstitution.SpecialType.IOStream);
                    default:
                        return null;
                }
            }

            // ::= S_
            if (ConsumeIf("_"))
            {
                if (SubstitutionList.Count != 0)
                    return SubstitutionList[0];
                return null;
            }

            //                ::= S <seq-id> _
            int SeqId = ParseSeqId();
            if (SeqId < 0)
                return null;
            SeqId++;
            if (!ConsumeIf("_") || SeqId >= SubstitutionList.Count)
                return null;
            return SubstitutionList[SeqId];
        }

        // NOTE: thoses data aren't used in the output
        //  <call-offset> ::= h <nv-offset> _
        //                ::= v <v-offset> _
        //  <nv-offset>   ::= <offset number>
        //                    # non-virtual base override
        //  <v-offset>    ::= <offset number> _ <virtual offset number>
        //                    # virtual base override, with vcall offset
        private bool ParseCallOffset()
        {
            if (ConsumeIf("h"))
            {
                return ParseNumber(true).Length == 0 || !ConsumeIf("_");
            }
            else if (ConsumeIf("v"))
            {
                return ParseNumber(true).Length == 0 || !ConsumeIf("_") || ParseNumber(true).Length == 0 || !ConsumeIf("_");
            }
            return true;
        }


        //   <class-enum-type> ::= <name>     # non-dependent type name, dependent type name, or dependent typename-specifier
        //                     ::= Ts <name>  # dependent elaborated type specifier using 'struct' or 'class'
        //                     ::= Tu <name>  # dependent elaborated type specifier using 'union'
        //                     ::= Te <name>  # dependent elaborated type specifier using 'enum'
        private BaseNode ParseClassEnumType()
        {
            string ElaboratedType = null;
            if (ConsumeIf("Ts"))
                ElaboratedType = "struct";
            else if (ConsumeIf("Tu"))
                ElaboratedType = "union";
            else if (ConsumeIf("Te"))
                ElaboratedType = "enum";
            BaseNode Name = ParseName();
            if (Name == null)
                return null;
            if (ElaboratedType == null)
                return Name;
            return new ElaboratedType(ElaboratedType, Name);
        }

        //  <function-type>         ::= [<CV-qualifiers>] [<exception-spec>] [Dx] F [Y] <bare-function-type> [<ref-qualifier>] E
        //  <bare-function-type>    ::= <signature type>+
        //                              # types are possible return type, then parameter types
        //  <exception-spec>        ::= Do                # non-throwing exception-specification (e.g., noexcept, throw())
        //                          ::= DO <expression> E # computed (instantiation-dependent) noexcept
        //                          ::= Dw <type>+ E      # dynamic exception specification with instantiation-dependent types
        private BaseNode ParseFunctionType()
        {
            CV CVQualifiers = ParseCVQualifiers();

            BaseNode ExceptionSpec = null;
            if (ConsumeIf("Do"))
            {
                ExceptionSpec = new NameType("noexcept");
            }
            else if (ConsumeIf("DO"))
            {
                BaseNode Expression = ParseExpression();
                if (Expression == null || !ConsumeIf("E"))
                    return null;
                ExceptionSpec = new NoexceptSpec(Expression);
            }
            else if (ConsumeIf("Dw"))
            {
                List<BaseNode> Types = new List<BaseNode>();
                while (!ConsumeIf("E"))
                {
                    BaseNode Type = ParseType();
                    if (Type == null)
                        return null;
                    Types.Add(Type);
                }

                ExceptionSpec = new DynamicExceptionSpec(new NodeArray(Types));
            }

            // We don't need the transaction
            ConsumeIf("Dx");
            if (!ConsumeIf("F"))
                return null;

            // extern "C"
            ConsumeIf("Y");
            BaseNode ReturnType = ParseType();
            if (ReturnType == null)
                return null;

            Reference ReferenceQualifier = Reference.None;
            List<BaseNode> Params = new List<BaseNode>();
            while (true)
            {
                if (ConsumeIf("E"))
                    break;
                if (ConsumeIf("v"))
                    continue;
                if (ConsumeIf("RE"))
                {
                    ReferenceQualifier = Reference.LValue;
                    break;
                }
                else if (ConsumeIf("OE"))
                {
                    ReferenceQualifier = Reference.RValue;
                    break;
                }

                BaseNode Type = ParseType();
                if (Type == null)
                    return null;
                Params.Add(Type);
            }
            return new FunctionType(ReturnType, new NodeArray(Params), new CVType(CVQualifiers, null), new SimpleReferenceType(ReferenceQualifier, null), ExceptionSpec);
        }

        //   <array-type> ::= A <positive dimension number> _ <element type>
        //                ::= A [<dimension expression>] _ <element type>
        private BaseNode ParseArrayType()
        {
            if (!ConsumeIf("A"))
                return null;
            BaseNode ElementType;
            if (char.IsDigit(Peek()))
            {
                string Dimension = ParseNumber();
                if (Dimension.Length == 0 || !ConsumeIf("_"))
                    return null;
                ElementType = ParseType();
                if (ElementType == null)
                    return null;
                return new ArrayType(ElementType, Dimension);
            }

            if (!ConsumeIf("_"))
            {
                BaseNode DimensionExpression = ParseExpression();
                if (DimensionExpression == null || !ConsumeIf("_"))
                    return null;
                ElementType = ParseType();
                if (ElementType == null)
                    return null;
                return new ArrayType(ElementType, DimensionExpression);
            }
            ElementType = ParseType();
            if (ElementType == null)
                return null;
            return new ArrayType(ElementType);
        }

        // <type>  ::= <builtin-type>
        //         ::= <qualified-type> (PARTIAL)
        //         ::= <function-type>
        //         ::= <class-enum-type>
        //         ::= <array-type> (TODO)
        //         ::= <pointer-to-member-type> (TODO)
        //         ::= <template-param>
        //         ::= <template-template-param> <template-args>
        //         ::= <decltype>
        //         ::= P <type>        # pointer
        //         ::= R <type>        # l-value reference
        //         ::= O <type>        # r-value reference (C++11)
        //         ::= C <type>        # complex pair (C99)
        //         ::= G <type>        # imaginary (C99)
        //         ::= <substitution>  # See Compression below
        private BaseNode ParseType(NameParserContext Context = null)
        {
            // Temporary context
            if (Context == null)
                Context = new NameParserContext();

            BaseNode Res = null;
            switch (Peek())
            {
                case 'r':
                case 'V':
                case 'K':
                    int TypePos = 0;
                    if (Peek(TypePos) == 'r')
                        TypePos++;
                    if (Peek(TypePos) == 'V')
                        TypePos++;
                    if (Peek(TypePos) == 'K')
                        TypePos++;
                    if (Peek(TypePos) == 'F' || (Peek(TypePos) == 'D' && (Peek(TypePos + 1) == 'o' || Peek(TypePos + 1) == 'O' || Peek(TypePos + 1) == 'w' || Peek(TypePos + 1) == 'x')))
                    {
                        Res = ParseFunctionType();
                        break;
                    }
                    CV CV = ParseCVQualifiers();
                    Res = ParseType(Context);
                    if (Res == null)
                        return null;
                    Res = new CVType(CV, Res);
                    break;
                case 'U':
                    // TODO: <extended-qualifier>
                    return null;
                case 'v':
                    Pos++;
                    return new NameType("void");
                case 'w':
                    Pos++;
                    return new NameType("wchar_t");
                case 'b':
                    Pos++;
                    return new NameType("bool");
                case 'c':
                    Pos++;
                    return new NameType("char");
                case 'a':
                    Pos++;
                    return new NameType("signed char");
                case 'h':
                    Pos++;
                    return new NameType("unsigned char");
                case 's':
                    Pos++;
                    return new NameType("short");
                case 't':
                    Pos++;
                    return new NameType("unsigned short");
                case 'i':
                    Pos++;
                    return new NameType("int");
                case 'j':
                    Pos++;
                    return new NameType("unsigned int");
                case 'l':
                    Pos++;
                    return new NameType("long");
                case 'm':
                    Pos++;
                    return new NameType("unsigned long");
                case 'x':
                    Pos++;
                    return new NameType("long long");
                case 'y':
                    Pos++;
                    return new NameType("unsigned long long");
                case 'n':
                    Pos++;
                    return new NameType("__int128");
                case 'o':
                    Pos++;
                    return new NameType("unsigned __int128");
                case 'f':
                    Pos++;
                    return new NameType("float");
                case 'd':
                    Pos++;
                    return new NameType("double");
                case 'e':
                    Pos++;
                    return new NameType("long double");
                case 'g':
                    Pos++;
                    return new NameType("__float128");
                case 'z':
                    Pos++;
                    return new NameType("...");
                case 'u':
                    Pos++;
                    return ParseSourceName();
                case 'D':
                    switch (Peek(1))
                    {
                        case 'd':
                            Pos += 2;
                            return new NameType("decimal64");
                        case 'e':
                            Pos += 2;
                            return new NameType("decimal128");
                        case 'f':
                            Pos += 2;
                            return new NameType("decimal32");
                        case 'h':
                            Pos += 2;
                            //return new NameType("decimal16");
                            // FIXME: GNU c++flit returns this but that is not what is supposed to be returned.
                            return new NameType("half");
                        case 'i':
                            Pos += 2;
                            return new NameType("char32_t");
                        case 's':
                            Pos += 2;
                            return new NameType("char16_t");
                        case 'a':
                            Pos += 2;
                            return new NameType("decltype(auto)");
                        case 'n':
                            Pos += 2;
                            //return new NameType("std::nullptr_t");
                            // FIXME: GNU c++flit returns this but that is not what is supposed to be returned.
                            return new NameType("decltype(nullptr)");
                        case 't':
                        case 'T':
                            Pos += 2;
                            Res = ParseDecltype();
                            break;
                        case 'o':
                        case 'O':
                        case 'w':
                        case 'x':
                            Res = ParseFunctionType();
                            break;
                        default:
                            return null;
                    }
                    break;
                case 'F':
                    Res = ParseFunctionType();
                    break;
                case 'A':
                    // TODO: <array-type>
                    return ParseArrayType();
                case 'M':
                    // TODO: <pointer-to-member-type>
                    Pos++;
                    return null;
                case 'T':
                    // might just be a class enum type
                    if (Peek(1) == 's' || Peek(1) == 'u' || Peek(1) == 'e')
                    {
                        Res = ParseClassEnumType();
                        break;
                    }
                    Res = ParseTemplateParam();
                    if (Res == null)
                        return null;

                    if (CanParseTemplateArgs && Peek() == 'I')
                    {
                        BaseNode TemplateArguments = ParseTemplateArguments();
                        if (TemplateArguments == null)
                            return null;
                        Res = new NameTypeWithTemplateArguments(Res, TemplateArguments);
                    }
                    break;
                case 'P':
                    Pos++;
                    Res = ParseType(Context);
                    if (Res == null)
                        return null;
                    Res = new PointerType(Res);
                    break;
                case 'R':
                    Pos++;
                    Res = ParseType(Context);
                    if (Res == null)
                        return null;
                    Res = new ReferenceType("&", Res);
                    break;
                case 'O':
                    Pos++;
                    Res = ParseType(Context);
                    if (Res == null)
                        return null;
                    Res = new ReferenceType("&&", Res);
                    break;
                case 'C':
                    Pos++;
                    Res = ParseType(Context);
                    if (Res == null)
                        return null;
                    Res = new PostfixQualifiedType(" complex", Res);
                    break;
                case 'G':
                    Pos++;
                    Res = ParseType(Context);
                    if (Res == null)
                        return null;
                    Res = new PostfixQualifiedType(" imaginary", Res);
                    break;
                case 'S':
                    if (Peek(1) != 't')
                    {
                        BaseNode Substitution = ParseSubstitution();
                        if (Substitution == null)
                            return null;
                        if (CanParseTemplateArgs && Peek() == 'I')
                        {
                            BaseNode TemplateArgument = ParseTemplateArgument();
                            if (TemplateArgument == null)
                                return null;
                            Res = new NameTypeWithTemplateArguments(Substitution, TemplateArgument);
                            break;
                        }
                        return Substitution;
                    }
                    else
                    {
                        Res = ParseClassEnumType();
                        break;
                    }
                default:
                    Res = ParseClassEnumType();
                    break;
            }
            if (Res != null)
                SubstitutionList.Add(Res);
            return Res;
        }

        // <special-name> ::= TV <type> # virtual table
        //                ::= TT <type> # VTT structure (construction vtable index)
        //                ::= TI <type> # typeinfo structure
        //                ::= TS <type> # typeinfo name (null-terminated byte string)
        //                ::= Tc <call-offset> <call-offset> <base encoding>
        //                ::= TW <object name> # Thread-local wrapper
        //                ::= TH <object name> # Thread-local initialization
        //                ::= T <call-offset> <base encoding>
        //                              # base is the nominal target function of thunk
        //                ::= GV <object name>	# Guard variable for one-time initialization
        private BaseNode ParseSpecialName(NameParserContext Context = null)
        {
            if (Peek() != 'T')
            {
                if (ConsumeIf("GV"))
                {
                    BaseNode Name = ParseName();
                    if (Name == null)
                        return null;
                    return new SpecialName("guard variable for ", Name);
                }
                return null;
            }

            BaseNode Node;
            switch (Peek(1))
            {
                // ::= TV <type>    # virtual table
                case 'V':
                    Pos += 2;
                    Node = ParseType(Context);
                    if (Node == null)
                        return null;
                    return new SpecialName("vtable for ", Node);
                // ::= TT <type>    # VTT structure (construction vtable index)
                case 'T':
                    Pos += 2;
                    Node = ParseType(Context);
                    if (Node == null)
                        return null;
                    return new SpecialName("VTT for ", Node);
                // ::= TI <type>    # typeinfo structure
                case 'I':
                    Pos += 2;
                    Node = ParseType(Context);
                    if (Node == null)
                        return null;
                    return new SpecialName("typeinfo for ", Node);
                // ::= TS <type> # typeinfo name (null-terminated byte string)
                case 'S':
                    Pos += 2;
                    Node = ParseType(Context);
                    if (Node == null)
                        return null;
                    return new SpecialName("typeinfo name for ", Node);
                // ::= Tc <call-offset> <call-offset> <base encoding>
                case 'c':
                    Pos += 2;
                    if (ParseCallOffset() || ParseCallOffset())
                        return null;
                    Node = ParseEncoding();
                    if (Node == null)
                        return null;
                    return new SpecialName("covariant return thunk to ", Node);
                // extension ::= TC <first type> <number> _ <second type>
                case 'C':
                    Pos += 2;
                    BaseNode FirstType = ParseType();
                    if (FirstType == null || ParseNumber(true).Length == 0 || !ConsumeIf("_"))
                        return null;
                    BaseNode SecondType = ParseType();
                    return new CtorVtableSpecialName(SecondType, FirstType);
                // ::= TH <object name> # Thread-local initialization
                case 'H':
                    Pos += 2;
                    Node = ParseName();
                    if (Node == null)
                        return null;
                    return new SpecialName("thread-local initialization routine for ", Node);
                // ::= TW <object name> # Thread-local wrapper
                case 'W':
                    Pos += 2;
                    Node = ParseName();
                    if (Node == null)
                        return null;
                    return new SpecialName("thread-local wrapper routine for ", Node);
                default:
                    Pos++;
                    bool IsVirtual = Peek() == 'v';
                    if (ParseCallOffset())
                        return null;
                    Node = ParseEncoding();
                    if (Node == null)
                        return null;
                    if (IsVirtual)
                        return new SpecialName("virtual thunk to ", Node);
                    return new SpecialName("non-virtual thunk to ", Node);
            }
        }

        // <CV-qualifiers>      ::= [r] [V] [K] # restrict (C99), volatile, const
        private CV ParseCVQualifiers()
        {
            CV Qualifiers = CV.None;
            if (ConsumeIf("r"))
            {
                Qualifiers |= CV.Restricted;
            }
            if (ConsumeIf("V"))
            {
                Qualifiers |= CV.Volatile;
            }
            if (ConsumeIf("K"))
            {
                Qualifiers |= CV.Const;
            }
            return Qualifiers;
        }


        // <ref-qualifier>      ::= R              # & ref-qualifier
        // <ref-qualifier>      ::= O              # && ref-qualifier
        private SimpleReferenceType ParseRefQualifiers()
        {
            Reference Res = Reference.None;
            if (ConsumeIf("O"))
            {
                Res = Reference.RValue;
            }
            else if (ConsumeIf("R"))
            {
                Res = Reference.LValue;
            }
            return new SimpleReferenceType(Res, null);
        }

        private BaseNode CreateNameNode(BaseNode Prev, BaseNode Name, NameParserContext Context)
        {
            BaseNode Res = Name;
            if (Prev != null)
                Res = new NestedName(Name, Prev);
            if (Context != null)
                Context.FinishWithTemplateArguments = false;
            return Res;
        }

        private int ParsePositiveNumber()
        {
            string Part = Mangled.Substring(Pos);
            int NumberLen;
            for (NumberLen = 0; NumberLen < Part.Length; NumberLen++)
            {
                if (!char.IsDigit(Part[NumberLen]))
                    break;
            }
            Pos += NumberLen;
            if (NumberLen == 0)
                return -1;
            return int.Parse(Part.Substring(0, NumberLen));
        }

        private string ParseNumber(bool IsSigned = false)
        {
            if (IsSigned)
                ConsumeIf("n");
            if (Count() == 0 || !char.IsDigit(Mangled[Pos]))
                return null;
            string Part = Mangled.Substring(Pos);
            int NumberLen;
            for (NumberLen = 0; NumberLen < Part.Length; NumberLen++)
            {
                if (!char.IsDigit(Part[NumberLen]))
                    break;
            }
            Pos += NumberLen;
            return Part.Substring(0, NumberLen);
        }

        // <source-name> ::= <positive length number> <identifier>
        private BaseNode ParseSourceName()
        {
            int Length = ParsePositiveNumber();
            if (Count() < Length || Length <= 0)
                return null;
            string Name = Mangled.Substring(Pos, Length);
            Pos += Length;
            if (Name.StartsWith("_GLOBAL__N"))
                return new NameType("(anonymous namespace)");
            return new NameType(Name);
        }

        // <operator-name> ::= nw    # new
        //                 ::= na    # new[]
        //                 ::= dl    # delete
        //                 ::= da    # delete[]
        //                 ::= ps    # + (unary)
        //                 ::= ng    # - (unary)
        //                 ::= ad    # & (unary)
        //                 ::= de    # * (unary)
        //                 ::= co    # ~
        //                 ::= pl    # +
        //                 ::= mi    # -
        //                 ::= ml    # *
        //                 ::= dv    # /
        //                 ::= rm    # %
        //                 ::= an    # &
        //                 ::= or    # |
        //                 ::= eo    # ^
        //                 ::= aS    # =
        //                 ::= pL    # +=
        //                 ::= mI    # -=
        //                 ::= mL    # *=
        //                 ::= dV    # /=
        //                 ::= rM    # %=
        //                 ::= aN    # &=
        //                 ::= oR    # |=
        //                 ::= eO    # ^=
        //                 ::= ls    # <<
        //                 ::= rs    # >>
        //                 ::= lS    # <<=
        //                 ::= rS    # >>=
        //                 ::= eq    # ==
        //                 ::= ne    # !=
        //                 ::= lt    # <
        //                 ::= gt    # >
        //                 ::= le    # <=
        //                 ::= ge    # >=
        //                 ::= ss    # <=>
        //                 ::= nt    # !
        //                 ::= aa    # &&
        //                 ::= oo    # ||
        //                 ::= pp    # ++ (postfix in <expression> context)
        //                 ::= mm    # -- (postfix in <expression> context)
        //                 ::= cm    # ,
        //                 ::= pm    # ->*
        //                 ::= pt    # ->
        //                 ::= cl    # ()
        //                 ::= ix    # []
        //                 ::= qu    # ?
        //                 ::= cv <type>    # (cast) (TODO)
        //                 ::= li <source-name>          # operator ""
        //                 ::= v <digit> <source-name>    # vendor extended operator (TODO)
        private BaseNode ParseOperatorName(NameParserContext Context)
        {
            switch (Peek())
            {
                case 'a':
                    switch (Peek(1))
                    {
                        case 'a':
                            Pos += 2;
                            return new NameType("operator&&");
                        case 'd':
                        case 'n':
                            Pos += 2;
                            return new NameType("operator&");
                        case 'N':
                            Pos += 2;
                            return new NameType("operator&=");
                        case 'S':
                            Pos += 2;
                            return new NameType("operator=");
                        default:
                            return null;
                    }
                case 'c':
                    switch (Peek(1))
                    {
                        case 'l':
                            Pos += 2;
                            return new NameType("operator()");
                        case 'm':
                            Pos += 2;
                            return new NameType("operator,");
                        case 'o':
                            Pos += 2;
                            return new NameType("operator~");
                        case 'v':
                            Pos += 2;
                            bool CanParseTemplateArgsBackup = CanParseTemplateArgs;
                            bool CanForwardTemplateReferenceBackup = CanForwardTemplateReference;
                            CanParseTemplateArgs = false;
                            CanForwardTemplateReference = CanForwardTemplateReferenceBackup || Context != null;

                            BaseNode Type = ParseType();

                            CanParseTemplateArgs = CanParseTemplateArgsBackup;
                            CanForwardTemplateReference = CanForwardTemplateReferenceBackup;
                            if (Type == null)
                                return null;
                            if (Context != null)
                                Context.CtorDtorConversion = true;
                            return new ConversionOperatorType(Type);
                        default:
                            return null;
                    }
                case 'd':
                    switch (Peek(1))
                    {
                        case 'a':
                            Pos += 2;
                            return new NameType("operator delete[]");
                        case 'e':
                            Pos += 2;
                            return new NameType("operator*");
                        case 'l':
                            Pos += 2;
                            return new NameType("operator delete");
                        case 'v':
                            Pos += 2;
                            return new NameType("operator/");
                        case 'V':
                            Pos += 2;
                            return new NameType("operator/=");
                        default:
                            return null;
                    }
                case 'e':
                    switch (Peek(1))
                    {
                        case 'o':
                            Pos += 2;
                            return new NameType("operator^");
                        case 'O':
                            Pos += 2;
                            return new NameType("operator^=");
                        case 'q':
                            Pos += 2;
                            return new NameType("operator==");
                        default:
                            return null;
                    }
                case 'g':
                    switch (Peek(1))
                    {
                        case 'e':
                            Pos += 2;
                            return new NameType("operator>=");
                        case 't':
                            Pos += 2;
                            return new NameType("operator>");
                        default:
                            return null;
                    }
                case 'i':
                    if (Peek(1) == 'x')
                    {
                        Pos += 2;
                        return new NameType("operator[]");
                    }
                    return null;
                case 'l':
                    switch (Peek(1))
                    {
                        case 'e':
                            Pos += 2;
                            return new NameType("operator<=");
                        case 'i':
                            Pos += 2;
                            BaseNode SourceName = ParseSourceName();
                            if (SourceName == null)
                                return null;
                            return new LiteralOperator(SourceName);
                        case 's':
                            Pos += 2;
                            return new NameType("operator<<");
                        case 'S':
                            Pos += 2;
                            return new NameType("operator<<=");
                        case 't':
                            Pos += 2;
                            return new NameType("operator<");
                        default:
                            return null;
                    }
                case 'm':
                    switch (Peek(1))
                    {
                        case 'i':
                            Pos += 2;
                            return new NameType("operator-");
                        case 'I':
                            Pos += 2;
                            return new NameType("operator-=");
                        case 'l':
                            Pos += 2;
                            return new NameType("operator*");
                        case 'L':
                            Pos += 2;
                            return new NameType("operator*=");
                        case 'm':
                            Pos += 2;
                            return new NameType("operator--");
                        default:
                            return null;
                    }
                case 'n':
                    switch (Peek(1))
                    {
                        case 'a':
                            Pos += 2;
                            return new NameType("operator new[]");
                        case 'e':
                            Pos += 2;
                            return new NameType("operator!=");
                        case 'g':
                            Pos += 2;
                            return new NameType("operator-");
                        case 't':
                            Pos += 2;
                            return new NameType("operator!");
                        case 'w':
                            Pos += 2;
                            return new NameType("operator new");
                        default:
                            return null;
                    }
                case 'o':
                    switch (Peek(1))
                    {
                        case 'o':
                            Pos += 2;
                            return new NameType("operator||");
                        case 'r':
                            Pos += 2;
                            return new NameType("operator|");
                        case 'R':
                            Pos += 2;
                            return new NameType("operator|=");
                        default:
                            return null;
                    }
                case 'p':
                    switch (Peek(1))
                    {
                        case 'm':
                            Pos += 2;
                            return new NameType("operator->*");
                        case 's':
                        case 'l':
                            Pos += 2;
                            return new NameType("operator+");
                        case 'L':
                            Pos += 2;
                            return new NameType("operator+=");
                        case 'p':
                            Pos += 2;
                            return new NameType("operator++");
                        case 't':
                            Pos += 2;
                            return new NameType("operator->");
                        default:
                            return null;
                    }
                case 'q':
                    if (Peek(1) == 'u')
                    {
                        Pos += 2;
                        return new NameType("operator?");
                    }
                    return null;
                case 'r':
                    switch (Peek(1))
                    {
                        case 'm':
                            Pos += 2;
                            return new NameType("operator%");
                        case 'M':
                            Pos += 2;
                            return new NameType("operator%=");
                        case 's':
                            Pos += 2;
                            return new NameType("operator>>");
                        case 'S':
                            Pos += 2;
                            return new NameType("operator>>=");
                        default:
                            return null;
                    }
                case 's':
                    if (Peek(1) == 's')
                    {
                        Pos += 2;
                        return new NameType("operator<=>");
                    }
                    return null;
                case 'v':
                    // TODO: ::= v <digit> <source-name>    # vendor extended operator
                    return null;
                default:
                    return null;
            }
        }

        // <unqualified-name> ::= <operator-name> [<abi-tags> (TODO)]
        //                    ::= <ctor-dtor-name> (TODO)
        //                    ::= <source-name>
        //                    ::= <unnamed-type-name> (TODO)
        //                    ::= DC <source-name>+ E      # structured binding declaration (TODO)
        private BaseNode ParseUnqualifiedName(NameParserContext Context)
        {
            BaseNode Res = null;
            char C = Peek();
            if (C == 'U')
            {
                // TODO: Unnamed Type Name
                // throw new Exception("Unnamed Type Name not implemented");
            }
            else if (char.IsDigit(C))
            {
                Res = ParseSourceName();
            }
            else if (ConsumeIf("DC"))
            {
                // TODO: Structured Binding Declaration
                // throw new Exception("Structured Binding Declaration not implemented");
            }
            else
            {
                Res = ParseOperatorName(Context);
            }

            if (Res != null)
            {
                // TODO: ABI Tags
                //throw new Exception("ABI Tags not implemented");
            }
            return Res;
        }

        // <ctor-dtor-name> ::= C1  # complete object constructor
        //                  ::= C2  # base object constructor
        //                  ::= C3  # complete object allocating constructor
        //                  ::= D0  # deleting destructor
        //                  ::= D1  # complete object destructor
        //                  ::= D2  # base object destructor 
        private BaseNode ParseCtorDtorName(NameParserContext Context, BaseNode Prev)
        {
            if (Prev.Type == NodeType.SpecialSubstitution && Prev is SpecialSubstitution)
            {
                ((SpecialSubstitution)Prev).SetExtended();
            }

            if (ConsumeIf("C"))
            {
                bool IsInherited = ConsumeIf("I");
                char C = Peek();

                if (C != '1' && C != '2' && C != '3')
                    return null;
                Pos++;
                if (Context != null)
                    Context.CtorDtorConversion = true;
                if (IsInherited && ParseName(Context) == null)
                    return null;
                return new CtorDtorNameType(Prev, false);
            }

            if (ConsumeIf("D"))
            {
                char C = Peek();
                if (C != '0' && C != '1' && C != '2')
                    return null;
                Pos++;
                if (Context != null)
                    Context.CtorDtorConversion = true;
                return new CtorDtorNameType(Prev, true);
            }
            return null;
        }

        // <function-param> ::= fp <top-level CV-qualifiers> _                                                                                           # L == 0, first parameter
        //                  ::= fp <top-level CV-qualifiers> <parameter-2 non-negative number> _                                                         # L == 0, second and later parameters
        //                  ::= fL <L-1 non-negative number> p <top-level CV-qualifiers> _                                                               # L > 0, first parameter
        //                  ::= fL <L-1 non-negative number> p <top-level CV-qualifiers> <parameter-2 non-negative number> _                             # L > 0, second and later parameters
        private BaseNode ParseFunctionParameter()
        {
            if (ConsumeIf("fp"))
            {
                // ignored
                ParseCVQualifiers();
                if (!ConsumeIf("_"))
                    return null;
                return new FunctionParameter(ParseNumber());
            }
            else if (ConsumeIf("fL"))
            {
                string L1Number = ParseNumber();
                if (L1Number == null || L1Number.Length == 0)
                    return null;
                if (!ConsumeIf("p"))
                    return null;
                // ignored
                ParseCVQualifiers();
                if (!ConsumeIf("_"))
                    return null;
                return new FunctionParameter(ParseNumber());

            }
            return null;
        }

        // <fold-expr> ::= fL <binary-operator-name> <expression> <expression>
        //             ::= fR <binary-operator-name> <expression> <expression>
        //             ::= fl <binary-operator-name> <expression>
        //             ::= fr <binary-operator-name> <expression>
        private BaseNode ParseFoldExpression()
        {
            if (!ConsumeIf("f"))
                return null;
            char FoldKind = Peek();
            bool HasInitializer = FoldKind == 'L' || FoldKind == 'R';
            bool IsLeftFold = FoldKind == 'l' || FoldKind == 'L';

            if (!IsLeftFold && !(FoldKind == 'r' || FoldKind == 'R'))
                return null;
            Pos++;

            string OperatorName = null;

            switch (PeekString(0, 2))
            {
                case "aa":
                    OperatorName = "&&";
                    break;
                case "an":
                    OperatorName = "&";
                    break;
                case "aN":
                    OperatorName = "&=";
                    break;
                case "aS":
                    OperatorName = "=";
                    break;
                case "cm":
                    OperatorName = ",";
                    break;
                case "ds":
                    OperatorName = ".*";
                    break;
                case "dv":
                    OperatorName = "/";
                    break;
                case "dV":
                    OperatorName = "/=";
                    break;
                case "eo":
                    OperatorName = "^";
                    break;
                case "eO":
                    OperatorName = "^=";
                    break;
                case "eq":
                    OperatorName = "==";
                    break;
                case "ge":
                    OperatorName = ">=";
                    break;
                case "gt":
                    OperatorName = ">";
                    break;
                case "le":
                    OperatorName = "<=";
                    break;
                case "ls":
                    OperatorName = "<<";
                    break;
                case "lS":
                    OperatorName = "<<=";
                    break;
                case "lt":
                    OperatorName = "<";
                    break;
                case "mi":
                    OperatorName = "-";
                    break;
                case "mI":
                    OperatorName = "-=";
                    break;
                case "ml":
                    OperatorName = "*";
                    break;
                case "mL":
                    OperatorName = "*=";
                    break;
                case "ne":
                    OperatorName = "!=";
                    break;
                case "oo":
                    OperatorName = "||";
                    break;
                case "or":
                    OperatorName = "|";
                    break;
                case "oR":
                    OperatorName = "|=";
                    break;
                case "pl":
                    OperatorName = "+";
                    break;
                case "pL":
                    OperatorName = "+=";
                    break;
                case "rm":
                    OperatorName = "%";
                    break;
                case "rM":
                    OperatorName = "%=";
                    break;
                case "rs":
                    OperatorName = ">>";
                    break;
                case "rS":
                    OperatorName = ">>=";
                    break;
                default:
                    return null;
            }
            Pos += 2;

            BaseNode Expression = ParseExpression();
            BaseNode Initializer = null;
            if (Expression == null)
                return null;
            if (HasInitializer)
            {
                Initializer = ParseExpression();
                if (Initializer == null)
                    return null;
            }
            if (IsLeftFold && Initializer != null)
            {
                BaseNode Temp = Expression;
                Expression = Initializer;
                Initializer = Temp;
            }
            return new FoldExpression(IsLeftFold, OperatorName, new PackedTemplateParameterExpansion(Expression), Initializer);
        }


        //                ::= cv <type> <expression>                               # type (expression), conversion with one argument
        //                ::= cv <type> _ <expression>* E                          # type (expr-list), conversion with other than one argument
        private BaseNode ParseConversionExpression()
        {
            if (!ConsumeIf("cv"))
                return null;

            bool CanParseTemplateArgsBackup = CanParseTemplateArgs;
            CanParseTemplateArgs = false;
            BaseNode Type = ParseType();
            CanParseTemplateArgs = CanParseTemplateArgsBackup;

            if (Type == null)
                return null;

            List<BaseNode> Expressions = new List<BaseNode>();
            if (ConsumeIf("_"))
            {
                while (!ConsumeIf("E"))
                {
                    BaseNode Expression = ParseExpression();
                    if (Expression == null)
                        return null;

                    Expressions.Add(Expression);
                }
            }
            else
            {
                BaseNode Expression = ParseExpression();
                if (Expression == null)
                    return null;

                Expressions.Add(Expression);
            }

            return new ConversionExpression(Type, new NodeArray(Expressions));
        }

        private BaseNode ParseBinaryExpression(string Name)
        {
            BaseNode LeftPart = ParseExpression();
            if (LeftPart == null)
                return null;

            BaseNode RightPart = ParseExpression();
            if (RightPart == null)
                return null;

            return new BinaryExpression(LeftPart, Name, RightPart);
        }

        private BaseNode ParsePrefixExpression(string Name)
        {
            BaseNode Expression = ParseExpression();
            if (Expression == null)
                return null;

            return new PrefixExpression(Name, Expression);
        }


        // <braced-expression> ::= <expression>
        //                     ::= di <field source-name> <braced-expression>    # .name = expr
        //                     ::= dx <index expression> <braced-expression>     # [expr] = expr
        //                     ::= dX <range begin expression> <range end expression> <braced-expression>
        //                                                                       # [expr ... expr] = expr
        private BaseNode ParseBracedExpression()
        {
            if (Peek() == 'd')
            {
                BaseNode BracedExpressionNode;
                switch (Peek(1))
                {
                    case 'i':
                        Pos += 2;
                        BaseNode Field = ParseSourceName();
                        if (Field == null)
                            return null;
                        BracedExpressionNode = ParseBracedExpression();
                        if (BracedExpressionNode == null)
                            return null;
                        return new BracedExpression(Field, BracedExpressionNode, false);
                    case 'x':
                        Pos += 2;
                        BaseNode Index = ParseExpression();
                        if (Index == null)
                            return null;
                        BracedExpressionNode = ParseBracedExpression();
                        if (BracedExpressionNode == null)
                            return null;
                        return new BracedExpression(Index, BracedExpressionNode, true);
                    case 'X':
                        Pos += 2;
                        BaseNode RangeBeginExpression = ParseExpression();
                        if (RangeBeginExpression == null)
                            return null;

                        BaseNode RangeEndExpression = ParseExpression();
                        if (RangeEndExpression == null)
                            return null;
                        BracedExpressionNode = ParseBracedExpression();
                        if (BracedExpressionNode == null)
                            return null;
                        return new BracedRangeExpression(RangeBeginExpression, RangeEndExpression, BracedExpressionNode);
                }
            }
            return ParseExpression();
        }

        //               ::= [gs] nw <expression>* _ <type> E                    # new (expr-list) type
        //               ::= [gs] nw <expression>* _ <type> <initializer>        # new (expr-list) type (init)
        //               ::= [gs] na <expression>* _ <type> E                    # new[] (expr-list) type
        //               ::= [gs] na <expression>* _ <type> <initializer>        # new[] (expr-list) type (init)
        //
        // <initializer> ::= pi <expression>* E                                  # parenthesized initialization
        private BaseNode ParseNewExpression()
        {
            bool IsGlobal = ConsumeIf("gs");
            bool IsArray = Peek(1) == 'a';

            if (!ConsumeIf("nw") || !ConsumeIf("na"))
                return null;

            List<BaseNode> Expressions = new List<BaseNode>();
            List<BaseNode> Initializers = new List<BaseNode>();

            while (!ConsumeIf("_"))
            {
                BaseNode Expression = ParseExpression();
                if (Expression == null)
                    return null;
                Expressions.Add(Expression);
            }
            BaseNode TypeNode = ParseType();
            if (TypeNode == null)
                return null;
            if (ConsumeIf("pi"))
            {
                while (!ConsumeIf("E"))
                {
                    BaseNode Initializer = ParseExpression();
                    if (Initializer == null)
                        return null;
                    Initializers.Add(Initializer);
                }
            }
            else if (!ConsumeIf("E"))
                return null;

            return new NewExpression(new NodeArray(Expressions), TypeNode, new NodeArray(Initializers), IsGlobal, IsArray);
        }


        // <expression> ::= <unary operator-name> <expression>
        //              ::= <binary operator-name> <expression> <expression>
        //              ::= <ternary operator-name> <expression> <expression> <expression>
        //              ::= pp_ <expression>                                     # prefix ++
        //              ::= mm_ <expression>                                     # prefix --
        //              ::= cl <expression>+ E                                   # expression (expr-list), call
        //              ::= cv <type> <expression>                               # type (expression), conversion with one argument
        //              ::= cv <type> _ <expression>* E                          # type (expr-list), conversion with other than one argument
        //              ::= tl <type> <braced-expression>* E                     # type {expr-list}, conversion with braced-init-list argument
        //              ::= il <braced-expression>* E                            # {expr-list}, braced-init-list in any other context
        //              ::= [gs] nw <expression>* _ <type> E                     # new (expr-list) type
        //              ::= [gs] nw <expression>* _ <type> <initializer>         # new (expr-list) type (init)
        //              ::= [gs] na <expression>* _ <type> E                     # new[] (expr-list) type
        //              ::= [gs] na <expression>* _ <type> <initializer>         # new[] (expr-list) type (init)
        //              ::= [gs] dl <expression>                                 # delete expression
        //              ::= [gs] da <expression>                                 # delete[] expression
        //              ::= dc <type> <expression>                               # dynamic_cast<type> (expression)
        //              ::= sc <type> <expression>                               # static_cast<type> (expression)
        //              ::= cc <type> <expression>                               # const_cast<type> (expression)
        //              ::= rc <type> <expression>                               # reinterpret_cast<type> (expression)
        //              ::= ti <type>                                            # typeid (type)
        //              ::= te <expression>                                      # typeid (expression)
        //              ::= st <type>                                            # sizeof (type)
        //              ::= sz <expression>                                      # sizeof (expression)
        //              ::= at <type>                                            # alignof (type)
        //              ::= az <expression>                                      # alignof (expression)
        //              ::= nx <expression>                                      # noexcept (expression)
        //              ::= <template-param>
        //              ::= <function-param>
        //              ::= dt <expression> <unresolved-name>                    # expr.name
        //              ::= pt <expression> <unresolved-name>                    # expr->name
        //              ::= ds <expression> <expression>                         # expr.*expr
        //              ::= sZ <template-param>                                  # sizeof...(T), size of a template parameter pack
        //              ::= sZ <function-param>                                  # sizeof...(parameter), size of a function parameter pack
        //              ::= sP <template-arg>* E                                 # sizeof...(T), size of a captured template parameter pack from an alias template
        //              ::= sp <expression>                                      # expression..., pack expansion
        //              ::= tw <expression>                                      # throw expression
        //              ::= tr                                                   # throw with no operand (rethrow)
        //              ::= <unresolved-name>                                    # f(p), N::f(p), ::f(p),
        //                                                                       # freestanding dependent name (e.g., T::x),
        //                                                                       # objectless nonstatic member reference
        //              ::= <expr-primary>
        private BaseNode ParseExpression()
        {
            bool IsGlobal = ConsumeIf("gs");
            BaseNode Expression = null;
            if (Count() < 2)
                return null;

            switch (Peek())
            {
                case 'L':
                    return ParseExpressionPrimary();
                case 'T':
                    return ParseTemplateParam();
                case 'f':
                    char C = Peek(1);
                    if (C == 'p' || (C == 'L' && char.IsDigit(Peek(2))))
                        return ParseFunctionParameter();
                    return ParseFoldExpression();
                case 'a':
                    switch (Peek(1))
                    {
                        case 'a':
                            Pos += 2;
                            return ParseBinaryExpression("&&");
                        case 'd':
                        case 'n':
                            Pos += 2;
                            return ParseBinaryExpression("&");
                        case 'N':
                            Pos += 2;
                            return ParseBinaryExpression("&=");
                        case 'S':
                            Pos += 2;
                            return ParseBinaryExpression("=");
                        case 't':
                            Pos += 2;
                            BaseNode Type = ParseType();
                            if (Type == null)
                                return null;
                            return new EnclosedExpression("alignof (", Type, ")");
                        case 'z':
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new EnclosedExpression("alignof (", Expression, ")");
                    }
                    return null;
                case 'c':
                    switch (Peek(1))
                    {
                        case 'c':
                            Pos += 2;
                            BaseNode To = ParseType();
                            if (To == null)
                                return null;
                            BaseNode From = ParseExpression();
                            if (From == null)
                                return null;
                            return new CastExpression("const_cast", To, From);
                        case 'l':
                            Pos += 2;
                            BaseNode Callee = ParseExpression();
                            if (Callee == null)
                                return null;
                            List<BaseNode> Names = new List<BaseNode>();
                            while (!ConsumeIf("E"))
                            {
                                Expression = ParseExpression();
                                if (Expression == null)
                                    return null;
                                Names.Add(Expression);
                            }
                            return new CallExpression(Callee, Names);
                        case 'm':
                            Pos += 2;
                            return ParseBinaryExpression(",");
                        case 'o':
                            Pos += 2;
                            return ParsePrefixExpression("~");
                        case 'v':
                            return ParseConversionExpression();
                    }
                    return null;
                case 'd':
                    BaseNode LeftNode = null;
                    BaseNode RightNode = null;
                    switch (Peek(1))
                    {
                        case 'a':
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return Expression;
                            return new DeleteExpression(Expression, IsGlobal, true);
                        case 'c':
                            Pos += 2;
                            BaseNode Type = ParseType();
                            if (Type == null)
                                return null;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return Expression;
                            return new CastExpression("dynamic_cast", Type, Expression);
                        case 'e':
                            Pos += 2;
                            return ParsePrefixExpression("*");
                        case 'l':
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new DeleteExpression(Expression, IsGlobal, false);
                        case 'n':
                            return ParseUnresolvedName();
                        case 's':
                            Pos += 2;
                            LeftNode = ParseExpression();
                            if (LeftNode == null)
                                return null;
                            RightNode = ParseExpression();
                            if (RightNode == null)
                                return null;
                            return new MemberExpression(LeftNode, ".*", RightNode);
                        case 't':
                            Pos += 2;
                            LeftNode = ParseExpression();
                            if (LeftNode == null)
                                return null;
                            RightNode = ParseExpression();
                            if (RightNode == null)
                                return null;
                            return new MemberExpression(LeftNode, ".", RightNode);
                        case 'v':
                            Pos += 2;
                            return ParseBinaryExpression("/");
                        case 'V':
                            Pos += 2;
                            return ParseBinaryExpression("/=");
                    }
                    return null;
                case 'e':
                    switch (Peek(1))
                    {
                        case 'o':
                            Pos += 2;
                            return ParseBinaryExpression("^");
                        case 'O':
                            Pos += 2;
                            return ParseBinaryExpression("^=");
                        case 'q':
                            Pos += 2;
                            return ParseBinaryExpression("==");
                    }
                    return null;
                case 'g':
                    switch (Peek(1))
                    {
                        case 'e':
                            Pos += 2;
                            return ParseBinaryExpression(">=");
                        case 't':
                            Pos += 2;
                            return ParseBinaryExpression(">");
                    }
                    return null;
                case 'i':
                    switch (Peek(1))
                    {
                        case 'x':
                            Pos += 2;
                            BaseNode Base = ParseExpression();
                            if (Base == null)
                                return null;
                            BaseNode Subscript = ParseExpression();
                            if (Base == null)
                                return null;
                            return new ArraySubscriptingExpression(Base, Subscript);
                        case 'l':
                            Pos += 2;

                            List<BaseNode> BracedExpressions = new List<BaseNode>();
                            while (!ConsumeIf("E"))
                            {
                                Expression = ParseBracedExpression();
                                if (Expression == null)
                                    return null;
                                BracedExpressions.Add(Expression);
                            }
                            return new InitListExpression(null, BracedExpressions);
                    }
                    return null;
                case 'l':
                    switch (Peek(1))
                    {
                        case 'e':
                            Pos += 2;
                            return ParseBinaryExpression("<=");
                        case 's':
                            Pos += 2;
                            return ParseBinaryExpression("<<");
                        case 'S':
                            Pos += 2;
                            return ParseBinaryExpression("<<=");
                        case 't':
                            Pos += 2;
                            return ParseBinaryExpression("<");
                    }
                    return null;
                case 'm':
                    switch (Peek(1))
                    {
                        case 'i':
                            Pos += 2;
                            return ParseBinaryExpression("-");
                        case 'I':
                            Pos += 2;
                            return ParseBinaryExpression("-=");
                        case 'l':
                            Pos += 2;
                            return ParseBinaryExpression("*");
                        case 'L':
                            Pos += 2;
                            return ParseBinaryExpression("*=");
                        case 'm':
                            Pos += 2;
                            if (ConsumeIf("_"))
                                return ParsePrefixExpression("--");
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new PostfixExpression(Expression, "--");
                    }
                    return null;
                case 'n':
                    switch (Peek(1))
                    {
                        case 'a':
                        case 'w':
                            Pos += 2;
                            return ParseNewExpression();
                        case 'e':
                            Pos += 2;
                            return ParseBinaryExpression("!=");
                        case 'g':
                            Pos += 2;
                            return ParsePrefixExpression("-");
                        case 't':
                            Pos += 2;
                            return ParsePrefixExpression("!");
                        case 'x':
                            Pos += 2;
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new EnclosedExpression("noexcept (", Expression, ")");
                    }
                    return null;
                case 'o':
                    switch (Peek(1))
                    {
                        case 'n':
                            return ParseUnresolvedName();
                        case 'o':
                            Pos += 2;
                            return ParseBinaryExpression("||");
                        case 'r':
                            Pos += 2;
                            return ParseBinaryExpression("|");
                        case 'R':
                            Pos += 2;
                            return ParseBinaryExpression("|=");
                    }
                    return null;
                case 'p':
                    switch (Peek(1))
                    {
                        case 'm':
                            Pos += 2;
                            return ParseBinaryExpression("->*");
                        case 'l':
                        case 's':
                            Pos += 2;
                            return ParseBinaryExpression("+");
                        case 'L':
                            Pos += 2;
                            return ParseBinaryExpression("+=");
                        case 'p':
                            Pos += 2;
                            if (ConsumeIf("_"))
                                return ParsePrefixExpression("++");
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new PostfixExpression(Expression, "++");
                        case 't':
                            Pos += 2;
                            LeftNode = ParseExpression();
                            if (LeftNode == null)
                                return null;
                            RightNode = ParseExpression();
                            if (RightNode == null)
                                return null;
                            return new MemberExpression(LeftNode, "->", RightNode);
                    }
                    return null;
                case 'q':
                    if (Peek(1) == 'u')
                    {
                        Pos += 2;
                        BaseNode Condition = ParseExpression();
                        if (Condition == null)
                            return null;
                        LeftNode = ParseExpression();
                        if (LeftNode == null)
                            return null;
                        RightNode = ParseExpression();
                        if (RightNode == null)
                            return null;
                        return new ConditionalExpression(Condition, LeftNode, RightNode);
                    }
                    return null;
                case 'r':
                    switch (Peek(1))
                    {
                        case 'c':
                            Pos += 2;
                            BaseNode To = ParseType();
                            if (To == null)
                                return null;
                            BaseNode From = ParseExpression();
                            if (From == null)
                                return null;
                            return new CastExpression("reinterpret_cast", To, From);
                        case 'm':
                            Pos += 2;
                            return ParseBinaryExpression("%");
                        case 'M':
                            Pos += 2;
                            return ParseBinaryExpression("%");
                        case 's':
                            Pos += 2;
                            return ParseBinaryExpression(">>");
                        case 'S':
                            Pos += 2;
                            return ParseBinaryExpression(">>=");
                    }
                    return null;
                case 's':
                    switch (Peek(1))
                    {
                        case 'c':
                            Pos += 2;
                            BaseNode To = ParseType();
                            if (To == null)
                                return null;
                            BaseNode From = ParseExpression();
                            if (From == null)
                                return null;
                            return new CastExpression("static_cast", To, From);
                        case 'p':
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new PackedTemplateParameterExpansion(Expression);
                        case 'r':
                            return ParseUnresolvedName();
                        case 't':
                            Pos += 2;
                            BaseNode EnclosedType = ParseType();
                            if (EnclosedType == null)
                                return null;
                            return new EnclosedExpression("sizeof (", EnclosedType, ")");
                        case 'z':
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new EnclosedExpression("sizeof (", Expression, ")");
                        case 'Z':
                            Pos += 2;
                            BaseNode SizeofParamNode = null;
                            switch (Peek())
                            {
                                case 'T':
                                    // FIXME: ??? Not entire sure if it's right
                                    SizeofParamNode = ParseFunctionParameter();
                                    if (SizeofParamNode == null)
                                        return null;
                                    return new EnclosedExpression("sizeof...(", new PackedTemplateParameterExpansion(SizeofParamNode), ")");
                                case 'f':
                                    SizeofParamNode = ParseFunctionParameter();
                                    if (SizeofParamNode == null)
                                        return null;
                                    return new EnclosedExpression("sizeof...(", SizeofParamNode, ")");
                            }
                            return null;
                        case 'P':
                            Pos += 2;
                            List<BaseNode> Arguments = new List<BaseNode>();
                            while (!ConsumeIf("E"))
                            {
                                BaseNode Argument = ParseTemplateArgument();
                                if (Argument == null)
                                    return null;
                                Arguments.Add(Argument);
                            }
                            return new EnclosedExpression("sizeof...(", new NodeArray(Arguments), ")");
                    }
                    return null;
                case 't':
                    switch (Peek(1))
                    {
                        case 'e':
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new EnclosedExpression("typeid (", Expression, ")");
                        case 't':
                            BaseNode EnclosedType = ParseExpression();
                            if (EnclosedType == null)
                                return null;
                            return new EnclosedExpression("typeid (", EnclosedType, ")");
                        case 'l':
                            Pos += 2;
                            BaseNode TypeNode = ParseType();
                            if (TypeNode == null)
                                return null;
                            List<BaseNode> BracedExpressions = new List<BaseNode>();
                            while (!ConsumeIf("E"))
                            {
                                Expression = ParseBracedExpression();
                                if (Expression == null)
                                    return null;
                                BracedExpressions.Add(Expression);
                            }
                            return new InitListExpression(TypeNode, BracedExpressions);
                        case 'r':
                            Pos += 2;
                            return new NameType("throw");
                        case 'w':
                            Pos += 2;
                            Expression = ParseExpression();
                            if (Expression == null)
                                return null;
                            return new ThrowExpression(Expression);
                    }
                    return null;
            }

            if (char.IsDigit(Peek()))
                return ParseUnresolvedName();
            return null;
        }

        private BaseNode ParseIntegerLiteral(string LiteralName)
        {
            string Number = ParseNumber(true);
            if (Number == null || Number.Length == 0 || !ConsumeIf("E"))
                return null;
            return new IntegerLiteral(LiteralName, Number);
        }

        // <expr-primary> ::= L <type> <value number> E                          # integer literal
        //                ::= L <type> <value float> E                           # floating literal (TODO)
        //                ::= L <string type> E                                  # string literal
        //                ::= L <nullptr type> E                                 # nullptr literal (i.e., "LDnE")
        //                ::= L <pointer type> 0 E                               # null pointer template argument
        //                ::= L <type> <real-part float> _ <imag-part float> E   # complex floating point literal (C 2000)
        //                ::= L _Z <encoding> E                                  # external name
        private BaseNode ParseExpressionPrimary()
        {
            if (!ConsumeIf("L"))
                return null;
            switch (Peek())
            {
                case 'w':
                    Pos++;
                    return ParseIntegerLiteral("wchar_t");
                case 'b':
                    if (ConsumeIf("b0E"))
                        return new NameType("false", NodeType.BooleanExpression);
                    if (ConsumeIf("b1E"))
                        return new NameType("true", NodeType.BooleanExpression);
                    return null;
                case 'c':
                    Pos++;
                    return ParseIntegerLiteral("char");
                case 'a':
                    Pos++;
                    return ParseIntegerLiteral("signed char");
                case 'h':
                    Pos++;
                    return ParseIntegerLiteral("unsigned char");
                case 's':
                    Pos++;
                    return ParseIntegerLiteral("short");
                case 't':
                    Pos++;
                    return ParseIntegerLiteral("unsigned short");
                case 'i':
                    Pos++;
                    return ParseIntegerLiteral("");
                case 'j':
                    Pos++;
                    return ParseIntegerLiteral("u");
                case 'l':
                    Pos++;
                    return ParseIntegerLiteral("l");
                case 'm':
                    Pos++;
                    return ParseIntegerLiteral("ul");
                case 'x':
                    Pos++;
                    return ParseIntegerLiteral("ll");
                case 'y':
                    Pos++;
                    return ParseIntegerLiteral("ull");
                case 'n':
                    Pos++;
                    return ParseIntegerLiteral("__int128");
                case 'o':
                    Pos++;
                    return ParseIntegerLiteral("unsigned __int128");
                case 'd':
                case 'e':
                case 'f':
                    // TODO: floating literal
                    return null;
                case '_':
                    if (ConsumeIf("_Z"))
                    {
                        BaseNode Encoding = ParseEncoding();
                        if (Encoding != null && ConsumeIf("E"))
                            return Encoding;
                    }
                    return null;
                case 'T':
                    return null;
                default:
                    BaseNode Type = ParseType();
                    if (Type == null)
                        return null;
                    string Number = ParseNumber();
                    if (Number == null || Number.Length == 0 || !ConsumeIf("E"))
                        return null;
                    return new IntegerCastExpression(Type, Number);
            }
        }

        // <decltype>  ::= Dt <expression> E  # decltype of an id-expression or class member access (C++0x)
        //             ::= DT <expression> E  # decltype of an expression (C++0x)
        private BaseNode ParseDecltype()
        {
            if (!ConsumeIf("D") || (!ConsumeIf("t") && !ConsumeIf("T")))
                return null;
            BaseNode Expression = ParseExpression();
            if (Expression == null)
                return null;
            if (!ConsumeIf("E"))
                return null;
            return new EnclosedExpression("decltype(", Expression, ")");
        }

        // <template-param>          ::= T_ # first template parameter
        //                           ::= T <parameter-2 non-negative number> _
        // <template-template-param> ::= <template-param>
        //                           ::= <substitution>
        private BaseNode ParseTemplateParam()
        {
            if (!ConsumeIf("T"))
                return null;
            int Index = 0;
            if (!ConsumeIf("_"))
            {
                Index = ParsePositiveNumber();
                if (Index < 0)
                    return null;
                Index++;
                if (!ConsumeIf("_"))
                    return null;
            }

            // 5.1.8: TODO: lambda?
            // if (IsParsingLambdaParameters)
            //    return new NameType("auto");

            if (CanForwardTemplateReference)
            {
                ForwardTemplateReference ForwardTemplateReference = new ForwardTemplateReference(Index);
                ForwardTemplateReferenceList.Add(ForwardTemplateReference);
                return ForwardTemplateReference;
            }
            if (Index >= TemplateParamList.Count)
                return null;
            return TemplateParamList[Index];
        }

        // <template-args> ::= I <template-arg>+ E
        private BaseNode ParseTemplateArguments(bool HasContext = false)
        {
            if (!ConsumeIf("I"))
                return null;
            if (HasContext)
                TemplateParamList.Clear();
            List<BaseNode> Args = new List<BaseNode>();
            while (!ConsumeIf("E"))
            {
                if (HasContext)
                {
                    List<BaseNode> TemplateParamListTemp = new List<BaseNode>(TemplateParamList);
                    BaseNode TemplateArgument = ParseTemplateArgument();
                    TemplateParamList = TemplateParamListTemp;
                    if (TemplateArgument == null)
                        return null;
                    Args.Add(TemplateArgument);
                    if (TemplateArgument.GetType().Equals(NodeType.PackedTemplateArgument))
                    {
                        TemplateArgument = new PackedTemplateParameter(((NodeArray)TemplateArgument).Nodes);
                    }
                    TemplateParamList.Add(TemplateArgument);
                }
                else
                {
                    BaseNode TemplateArgument = ParseTemplateArgument();
                    if (TemplateArgument == null)
                        return null;
                    Args.Add(TemplateArgument);
                }
            }
            return new TemplateArguments(Args);
        }


        // <template-arg> ::= <type>                                             # type or template
        //                ::= X <expression> E                                   # expression
        //                ::= <expr-primary>                                     # simple expressions
        //                ::= J <template-arg>* E                                # argument pack
        private BaseNode ParseTemplateArgument()
        {
            switch (Peek())
            {
                // X <expression> E
                case 'X':
                    Pos++;
                    BaseNode Expression = ParseExpression();
                    if (Expression == null || !ConsumeIf("E"))
                        return null;
                    return Expression;
                // <expr-primary>
                case 'L':
                    return ParseExpressionPrimary();
                // J <template-arg>* E
                case 'J':
                    Pos++;
                    List<BaseNode> TemplateArguments = new List<BaseNode>();
                    while (!ConsumeIf("E"))
                    {
                        BaseNode TemplateArgument = ParseTemplateArgument();
                        if (TemplateArgument == null)
                            return null;
                        TemplateArguments.Add(TemplateArgument);
                    }
                    return new NodeArray(TemplateArguments, NodeType.PackedTemplateArgument);
                // <type>
                default:
                    return ParseType();
            }
        }

        class NameParserContext
        {
            public CVType CV;
            public SimpleReferenceType Ref;
            public bool FinishWithTemplateArguments;
            public bool CtorDtorConversion;
        }


        //   <unresolved-type> ::= <template-param> [ <template-args> ]            # T:: or T<X,Y>::
        //                     ::= <decltype>                                      # decltype(p)::
        //                     ::= <substitution>
        private BaseNode ParseUnresolvedType()
        {
            if (Peek() == 'T')
            {
                BaseNode TemplateParam = ParseTemplateParam();
                if (TemplateParam == null)
                    return null;
                SubstitutionList.Add(TemplateParam);
                return TemplateParam;
            }
            else if (Peek() == 'D')
            {
                BaseNode DeclType = ParseDecltype();
                if (DeclType == null)
                    return null;
                SubstitutionList.Add(DeclType);
                return DeclType;
            }
            return ParseSubstitution();
        }

        // <simple-id> ::= <source-name> [ <template-args> ]
        private BaseNode ParseSimpleId()
        {
            BaseNode SourceName = ParseSourceName();
            if (SourceName == null)
                return null;
            if (Peek() == 'I')
            {
                BaseNode TemplateArguments = ParseTemplateArguments();
                if (TemplateArguments == null)
                    return null;
                return new NameTypeWithTemplateArguments(SourceName, TemplateArguments);
            }
            return SourceName;
        }

        //  <destructor-name> ::= <unresolved-type>                               # e.g., ~T or ~decltype(f())
        //                    ::= <simple-id>                                     # e.g., ~A<2*N>
        private BaseNode ParseDestructorName()
        {
            BaseNode Node;
            if (char.IsDigit(Peek()))
            {
                Node = ParseSimpleId();
            }
            else
            {
                Node = ParseUnresolvedType();
            }
            if (Node == null)
                return null;
            return new DtorName(Node);
        }

        //  <base-unresolved-name> ::= <simple-id>                                # unresolved name
        //  extension              ::= <operator-name>                            # unresolved operator-function-id
        //  extension              ::= <operator-name> <template-args>            # unresolved operator template-id
        //                         ::= on <operator-name>                         # unresolved operator-function-id
        //                         ::= on <operator-name> <template-args>         # unresolved operator template-id
        //                         ::= dn <destructor-name>                       # destructor or pseudo-destructor;
        //                                                                        # e.g. ~X or ~X<N-1>
        private BaseNode ParseBaseUnresolvedName()
        {
            if (char.IsDigit(Peek()))
                return ParseSimpleId();
            else if (ConsumeIf("dn"))
                return ParseDestructorName();
            ConsumeIf("on");
            BaseNode OperatorName = ParseOperatorName(null);
            if (OperatorName == null)
                return null;
            if (Peek() == 'I')
            {
                BaseNode TemplateArguments = ParseTemplateArguments();
                if (TemplateArguments == null)
                    return null;
                return new NameTypeWithTemplateArguments(OperatorName, TemplateArguments);
            }
            return OperatorName;
        }

        // <unresolved-name> ::= [gs] <base-unresolved-name>                     # x or (with "gs") ::x
        //                   ::= sr <unresolved-type> <base-unresolved-name>     # T::x / decltype(p)::x
        //                   ::= srN <unresolved-type> <unresolved-qualifier-level>+ E <base-unresolved-name>
        //                                                                       # T::N::x /decltype(p)::N::x
        //                   ::= [gs] sr <unresolved-qualifier-level>+ E <base-unresolved-name>
        //                                                                       # A::x, N::y, A<T>::z; "gs" means leading "::"
        private BaseNode ParseUnresolvedName(NameParserContext Context = null)
        {
            BaseNode Res = null;
            if (ConsumeIf("srN"))
            {
                Res = ParseUnresolvedType();
                if (Res == null)
                    return null;

                if (Peek() == 'I')
                {
                    BaseNode TemplateArguments = ParseTemplateArguments();
                    if (TemplateArguments == null)
                        return null;
                    Res = new NameTypeWithTemplateArguments(Res, TemplateArguments);
                    if (Res == null)
                        return null;
                }

                while (!ConsumeIf("E"))
                {
                    BaseNode SimpleId = ParseSimpleId();
                    if (SimpleId == null)
                        return null;
                    Res = new QualifiedName(Res, SimpleId);
                    if (Res == null)
                        return null;

                }

                BaseNode BaseName = ParseBaseUnresolvedName();
                if (BaseName == null)
                    return null;
                return new QualifiedName(Res, BaseName);
            }

            bool IsGlobal = ConsumeIf("gs");

            // ::= [gs] <base-unresolved-name>                     # x or (with "gs") ::x
            if (!ConsumeIf("sr"))
            {
                Res = ParseBaseUnresolvedName();
                if (Res == null)
                    return null;
                if (IsGlobal)
                    Res = new GlobalQualifiedName(Res);
                return Res;
            }

            // ::= [gs] sr <unresolved-qualifier-level>+ E <base-unresolved-name>
            if (char.IsDigit(Peek()))
            {
                do
                {
                    BaseNode Qualifier = ParseSimpleId();
                    if (Qualifier == null)
                        return null;
                    if (Res != null)
                        Res = new QualifiedName(Res, Qualifier);
                    else if (IsGlobal)
                        Res = new GlobalQualifiedName(Qualifier);
                    else
                        Res = Qualifier;
                    if (Res == null)
                        return null;
                } while (!ConsumeIf("E"));
            }
            // ::= sr <unresolved-type> [tempate-args] <base-unresolved-name>     # T::x / decltype(p)::x
            else
            {
                Res = ParseUnresolvedType();
                if (Res == null)
                    return null;

                if (Peek() == 'I')
                {
                    BaseNode TemplateArguments = ParseTemplateArguments();
                    if (TemplateArguments == null)
                        return null;
                    Res = new NameTypeWithTemplateArguments(Res, TemplateArguments);
                    if (Res == null)
                        return null;
                }
            }

            if (Res == null)
                return null;

            BaseNode BaseUnresolvedName = ParseBaseUnresolvedName();
            if (BaseUnresolvedName == null)
                return null;

            return new QualifiedName(Res, BaseUnresolvedName);
        }

        //    <unscoped-name> ::= <unqualified-name>
        //                    ::= St <unqualified-name>   # ::std::
        private BaseNode ParseUnscopedName(NameParserContext Context)
        {
            if (ConsumeIf("St"))
            {
                BaseNode UnresolvedName = ParseUnresolvedName(Context);
                if (UnresolvedName == null)
                    return null;
                return new StdQualifiedName(UnresolvedName);
            }
            return ParseUnresolvedName(Context);
        }

        // <nested-name> ::= N [<CV-qualifiers>] [<ref-qualifier>] <prefix (TODO)> <unqualified-name> E
        //               ::= N [<CV-qualifiers>] [<ref-qualifier>] <template-prefix (TODO)> <template-args (TODO)> E
        private BaseNode ParseNestedName(NameParserContext Context)
        {
            // Impossible in theory
            if (Consume() != 'N')
                return null;
            BaseNode Res = null;
            CVType CV = new CVType(ParseCVQualifiers(), null);
            if (Context != null)
                Context.CV = CV;
            SimpleReferenceType Ref = ParseRefQualifiers();
            if (Context != null)
                Context.Ref = Ref;

            if (ConsumeIf("St"))
                Res = new NameType("std");

            while (!ConsumeIf("E"))
            {
                // <data-member-prefix> end
                if (ConsumeIf("M"))
                {
                    if (Res == null)
                        return null;
                    continue;
                }
                char C = Peek();

                // TODO: template args
                if (C == 'T')
                {
                    BaseNode TemplateParam = ParseTemplateParam();
                    if (TemplateParam == null)
                        return null;
                    Res = CreateNameNode(Res, TemplateParam, Context);
                    SubstitutionList.Add(Res);
                    continue;
                }

                // <template-prefix> <template-args>
                if (C == 'I')
                {
                    BaseNode TemplateArgument = ParseTemplateArguments(Context != null);
                    if (TemplateArgument == null || Res == null)
                        return null;
                    Res = new NameTypeWithTemplateArguments(Res, TemplateArgument);
                    if (Context != null)
                        Context.FinishWithTemplateArguments = true;
                    SubstitutionList.Add(Res);
                    continue;
                }

                // <decltype>
                if (C == 'D' && (Peek(1) == 't' || Peek(1) == 'T'))
                {
                    BaseNode Decltype = ParseDecltype();
                    if (Decltype == null)
                        return null;
                    Res = CreateNameNode(Res, Decltype, Context);
                    SubstitutionList.Add(Res);
                    continue;
                }

                // <substitution>
                if (C == 'S' && Peek(1) != 't')
                {
                    BaseNode Substitution = ParseSubstitution();
                    if (Substitution == null)
                        return null;
                    Res = CreateNameNode(Res, Substitution, Context);
                    if (Res != Substitution)
                        SubstitutionList.Add(Substitution);
                    continue;
                }

                // <ctor-dtor-name> of ParseUnqualifiedName
                if (C == 'C' || (C == 'D' && Peek(1) != 'C'))
                {
                    // We cannot have nothing before this
                    if (Res == null)
                        return null;
                    BaseNode CtOrDtorName = ParseCtorDtorName(Context, Res);

                    if (CtOrDtorName == null)
                        return null;
                    Res = CreateNameNode(Res, CtOrDtorName, Context);

                    // TODO: ABI Tags (before)
                    if (Res == null)
                        return null;

                    SubstitutionList.Add(Res);
                    continue;
                }

                BaseNode UnqualifiedName = ParseUnqualifiedName(Context);
                if (UnqualifiedName == null)
                {
                    return null;
                }
                Res = CreateNameNode(Res, UnqualifiedName, Context);

                SubstitutionList.Add(Res);
            }
            if (Res == null || SubstitutionList.Count == 0)
                return null;

            SubstitutionList.RemoveAt(SubstitutionList.Count - 1);
            return Res;
        }

        //   <discriminator> ::= _ <non-negative number>      # when number < 10
        //                   ::= __ <non-negative number> _   # when number >= 10
        private void ParseDiscriminator()
        {
            if (Count() == 0)
                return;
            // We ignore the discriminator, we don't need it.
            if (ConsumeIf("_"))
            {
                ConsumeIf("_");
                while (char.IsDigit(Peek()) && Count() != 0)
                {
                    Consume();
                }
                ConsumeIf("_");
            }
        }

        //   <local-name> ::= Z <function encoding> E <entity name> [<discriminator>]
        //                ::= Z <function encoding> E s [<discriminator>]
        //                ::= Z <function encoding> Ed [ <parameter number> ] _ <entity name>
        private BaseNode ParseLocalName(NameParserContext Context)
        {
            if (!ConsumeIf("Z"))
                return null;
            BaseNode Encoding = ParseEncoding();
            if (Encoding == null || !ConsumeIf("E"))
                return null;
            BaseNode EntityName;
            if (ConsumeIf("s"))
            {
                ParseDiscriminator();
                return new LocalName(Encoding, new NameType("string literal"));
            }
            else if (ConsumeIf("d"))
            {
                ParseNumber(true);
                if (!ConsumeIf("_"))
                    return null;
                EntityName = ParseName(Context);
                if (EntityName == null)
                    return null;
                return new LocalName(Encoding, EntityName);
            }

            EntityName = ParseName(Context);
            if (EntityName == null)
                return null;
            ParseDiscriminator();
            return new LocalName(Encoding, EntityName);
        }

        // <name> ::= <nested-name>
        //        ::= <unscoped-name>
        //        ::= <unscoped-template-name> <template-args>
        //        ::= <local-name>  # See Scope Encoding below (TODO)
        private BaseNode ParseName(NameParserContext Context = null)
        {
            ConsumeIf("L");

            if (Peek() == 'N')
                return ParseNestedName(Context);

            if (Peek() == 'Z')
                return ParseLocalName(Context);

            if (Peek() == 'S' && Peek(1) != 't')
            {
                BaseNode Substitution = ParseSubstitution();
                if (Substitution == null)
                    return null;
                if (Peek() != 'I')
                    return null;
                BaseNode TemplateArguments = ParseTemplateArguments(Context != null);
                if (TemplateArguments == null)
                    return null;
                if (Context != null)
                    Context.FinishWithTemplateArguments = true;
                return new NameTypeWithTemplateArguments(Substitution, TemplateArguments);
            }

            BaseNode Res = ParseUnscopedName(Context);
            if (Res == null)
                return null;

            if (Peek() == 'I')
            {
                SubstitutionList.Add(Res);
                BaseNode TemplateArguments = ParseTemplateArguments(Context != null);
                if (TemplateArguments == null)
                    return null;
                if (Context != null)
                    Context.FinishWithTemplateArguments = true;
                return new NameTypeWithTemplateArguments(Res, TemplateArguments);
            }

            return Res;
        }

        private bool IsEncodingEnd()
        {
            char C = Peek();
            return Count() == 0 || C == 'E' || C == '.' || C == '_';
        }

        // <encoding> ::= <function name> <bare-function-type>
        //            ::= <data name>
        //            ::= <special-name>
        private BaseNode ParseEncoding()
        {
            NameParserContext Context = new NameParserContext();
            if (Peek() == 'T' || (Peek() == 'G' && Peek(1) == 'V'))
                return ParseSpecialName(Context);
            BaseNode Name = ParseName(Context);
            if (Name == null)
                return null;

            // TODO: compute template refs here

            if (IsEncodingEnd())
                return Name;

            // TODO: Ua9enable_ifI

            BaseNode ReturnType = null;
            if (!Context.CtorDtorConversion && Context.FinishWithTemplateArguments)
            {
                ReturnType = ParseType();
                if (ReturnType == null)
                    return null;
            }

            if (ConsumeIf("v"))
                return new EncodedFunction(Name, null, Context.CV, Context.Ref, null, ReturnType);

            List<BaseNode> Params = new List<BaseNode>();

            // backup because that can be destroyed by parseType
            CVType CV = Context.CV;
            SimpleReferenceType Ref = Context.Ref;

            while (!IsEncodingEnd())
            {
                BaseNode Param = ParseType();
                if (Param == null)
                    return null;
                Params.Add(Param);
            }

            return new EncodedFunction(Name, new NodeArray(Params), CV, Ref, null, ReturnType);
        }

        // <mangled-name> ::= _Z <encoding>
        //                ::= <type>
        private BaseNode Parse()
        {
            if (ConsumeIf("_Z"))
            {
                BaseNode Encoding = ParseEncoding();
                if (Encoding != null && Count() == 0)
                {
                    return Encoding;
                }
                return null;
            }
            else
            {
                BaseNode Type = ParseType();
                if (Type != null && Count() == 0)
                {
                    return Type;
                }
                return null;
            }
        }

        public static string Parse(string OriginalMangled)
        {
            Demangler Instance = new Demangler(OriginalMangled);
            BaseNode ResNode = Instance.Parse();
            if (ResNode != null)
            {
                StringWriter Writer = new StringWriter();
                ResNode.Print(Writer);
                return Writer.ToString();
            }
            return OriginalMangled;
        }
    }
}