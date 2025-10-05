ClientUser Entity

Purpose
- External user belonging to a Client, with optional contact details and role name.

Properties
- Id: `ClientUserId`
- ClientId: `ClientId`
- DisplayName: `DisplayName` (local VO)
- Email?: `Email`
- Phone?: `Phone`
- RoleName?: `RoleName`
- CreatedAt: `DateTime` (UTC)
- UpdatedAt?: `DateTime` (UTC)

API Notes
- Factory returns `Result<ClientUser>`; `Update` changes profile fields.
- No raw strings; all text inputs use VOs.


