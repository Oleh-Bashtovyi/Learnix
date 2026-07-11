namespace Learnix.Infrastructure;

// Marker class for IStringLocalizer<EmailStrings> — the .resx files in Email/Resources/ are bound to it
// by name, so it has to be an empty class: an interface cannot be a resource root, and any member here
// would be dead weight (S2094 does not apply).
#pragma warning disable S2094
public sealed class EmailStrings { }
#pragma warning restore S2094
