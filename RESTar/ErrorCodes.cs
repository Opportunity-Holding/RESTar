#pragma warning disable 1591
namespace RESTar
{
    /// <summary>
    /// The error codes used by RESTar
    /// </summary>
    public enum ErrorCodes
    {
        NoError = -1,

        // 000: Unknown error
        Unknown = 000,

        // - - - - - - - - - 
        // 001-099: Request errors
        // 000-009: URI errors
        InvalidUriSyntax = 001,
        InvalidMetaConditionValueType = 002,
        InvalidMetaConditionOperator = 003,
        InvalidMetaConditionSyntax = 004,
        InvalidMetaConditionKey = 005,
        InvalidConditionSyntax = 006,
        InvalidConditionOperator = 007,
        InvalidEventSelector = 008,
        InvalidConditionValueType = 009,

        // 010-019: Data source syntax and format errors
        FailedJsonDeserialization = 010,
        ExcelReaderError = 011,
        DataSourceFormat = 012,
        NoDataSource = 013,
        UnsupportedContent = 014,
        UnknownFormatter = 015,
        NotAcceptable = 016,
        UpgradeRequired = 017,
        UnknownWebSocketId = 018,
        InvalidEnumValue = 019,

        // 020-029: Headers error
        InvalidSourceData = 020,
        InvalidSource = 021,
        InvalidDestination = 022,

        UnknownProtocol = 030,

        // - - - - - - - - - 
        // 100-199: Resource errors
        // 100-109: Resource locator errors
        UnknownResource = 100,
        UnknownResourceForMapping = 101,
        AmbiguousResource = 102,
        AmbiguousMatch = 103,
        InvalidResourceEntity = 104,
        ResourceIsInternal = 105,
        UnknownResourceView = 106,
        UnknownMacro = 107,
        WrongResourceKind = 108,

        // 110-119: Property locator errors
        UnknownProperty = 110,
        AmbiguousProperty = 111,
        UnknownPropertyOfGeneratedObject = 112,
        FailedBinding = 113,

        // 120-129: Resource registration errors
        VirtualResourceMissingInterfaceImplementation = 120,
        InvalidResourceMember = 121,
        NoAvalailableDynamicTable = 122,
        InvalidEventDeclaration = 123,
        InvalidResourceDeclaration = 124,
        InvalidResourceViewDeclaration = 125,
        InvalidTerminalDeclaration = 126,
        InvalidReferencedEnumDeclaration = 127,
        InvalidBinaryResourceDeclaration = 128,
        InvalidResourceControllerDeclaration = 129,

        // 130-139: Alias errors
        AliasAlreadyInUse = 130,
        AliasEqualToResourceName = 131,

        // 200-300: Handler errors
        AbortedSelect = 201,
        AbortedInsert = 202,
        AbortedUpdate = 203,
        AbortedDelete = 204,
        AbortedReport = 205,
        NotSignedIn = 210,
        NotAuthorized = 211,
        NoMatchingHtml = 212,
        FailedResourceAuthentication = 213,
        MethodNotAllowed = 214,
        Untraceable = 215,
        MissingUri = 216,
        ReusedContext = 217,

        // 300-400: Database errors
        DatabaseError = 300,
        AbortedByCommitHook = 301,

        // 400-500: Initialization errors
        NotInitialized = 400,
        AddOnError = 401,
        EntityResourceProviderError = 402,
        ResourceWrapperError = 403,
        InfiniteLoopDetected = 404,
        NotImplemented = 405,
        MissingConfigurationFile = 407,
        InvalidProtocolProvider = 408,
        InvalidContentTypeProvider = 408,

        // 500-600: Other errors
        NotCompliantWithProtocol = 500,
        WebSocketNotConnected = 501,
        InvalidShellHeaderAssignmentSyntax = 502,
        ShellError = 503,
        NoResponseFromRemoteService = 504,
        ExternalServiceNotRESTar = 505,
        WebSocketMessageTooLarge = 506,
        UnreadableContentStream = 507,
        UnknownEventType = 508
    }
}