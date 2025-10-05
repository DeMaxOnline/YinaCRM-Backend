User Entity

Purpose
- Internal user representation with auth subject, display name, contact email and optional locale/timezone.

Properties
- Id: `UserId`
- AuthSub: `AuthSubject`
- DisplayName: `DisplayName` (local VO)
- Email: `Email`
- TimeZone?: `TimeZoneId`
- Locale?: `LocaleCode`
- CreatedAt: `DateTime` (UTC)
- UpdatedAt?: `DateTime` (UTC)

API Notes
- Factory returns `Result<User>`; `UpdateProfile` updates display name, email and preferences.
- No raw strings; all text inputs use VOs.


