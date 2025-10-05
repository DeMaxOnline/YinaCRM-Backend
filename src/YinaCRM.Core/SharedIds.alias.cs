// Shared ID type aliases used across aggregates
global using UserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.User.UserIdTag>;
global using ClientUserId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.Entities.ClientUser.ClientUserIdTag>;
global using SupportTicketId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.SupportTicketIdTag>;
global using InteractionId = Yina.Common.Foundation.Ids.StrongId<YinaCRM.Core.InteractionIdTag>;

namespace YinaCRM.Core;

public readonly struct SupportTicketIdTag { }
public readonly struct InteractionIdTag { }
