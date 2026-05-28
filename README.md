# CommunityService

CommunityService håndterer det sociale fællesskab i FitLife Digital. Servicen giver medlemmer mulighed for at interagere med hinanden via opslag og kommentarer, opdelt i globale opslag og center-specifikke opslag.

## Funktionalitet

- **Globale opslag** – synlige for alle medlemmer på tværs af FitLifes seks centre
- **Center-specifikke opslag** – kun synlige for medlemmer tilknyttet et specifikt center
- **Kommentarer** – medlemmer kan kommentere på opslag
- **Admin panel** – admins kan oprette og slette opslag via CommunityAdmin siden

## Center IDs

De seks FitLife centre har følgende faste IDs:

| Center | ID |
|--------|-----|
| FitLife Aarhus C | `00000000-0000-0000-0000-000000000001` |
| FitLife Aarhus Nord | `00000000-0000-0000-0000-000000000002` |
| FitLife Viby | `00000000-0000-0000-0000-000000000003` |
| FitLife Randers | `00000000-0000-0000-0000-000000000004` |
| FitLife Horsens | `00000000-0000-0000-0000-000000000005` |
| FitLife Silkeborg | `00000000-0000-0000-0000-000000000006` |

## Hvordan center-feed fungerer

Når et medlem tilgår `/Community` henter servicen automatisk medlemmets `HomeCenterId` fra MemberService via:
GET http://haav-member-service:8080/api/members/by-account/{userAccountId}

Herefter hentes center-specifikke opslag for det pågældende center samt globale opslag.

## Routing via nginx
/Community        → Razor Page for medlemmer
/CommunityAdmin   → Razor Page for admins
/api/community/   → REST API endpoints

## Miljøvariabler

| Variabel | Beskrivelse |
|----------|-------------|
| `Vault__Url` | URL til HashiCorp Vault |
| `Vault__Token` | Token til Vault autentificering |
| `GatewayUrl` | Intern URL til community service selv  |
| `MemberServiceUrl` | URL til MemberService  |
| `Loki__Url` | URL til Grafana Loki til logging |

## API Kontrakt

Swagger dokumentation tilgængelig på `/swagger` når servicen kører.
