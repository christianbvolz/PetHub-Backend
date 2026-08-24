# 🐾 PetHub - Backend API

O PetHub é uma plataforma que conecta pessoas que desejam adotar animais de estimação com donos ou abrigos que possuem animais para adoção. Este repositório contém o Backend (API) da aplicação, construído com tecnologias modernas do ecossistema .NET.

[![CI](https://github.com/christianbvolz/PetHub-Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/christianbvolz/PetHub-Backend/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-368%20passing-brightgreen)](tests/)
[![Coverage](https://img.shields.io/badge/coverage-%E2%89%A580%25%20CI-brightgreen)](tests/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 🚀 Tecnologias Utilizadas

- **Linguagem:** C# (.NET 8)
- **Framework:** ASP.NET Core Web API (controllers)
- **Banco de Dados:** MySQL (Hospedado no TiDB Cloud Serverless)
- **ORM:** Entity Framework Core 8 (Pomelo MySQL Provider) + migrations
- **Tempo Real:** SignalR (chat persistido e notificações in-app)
- **Segurança:** BCrypt (hash de senhas), JWT + refresh token rotativo, rate limiting
- **Logging:** Serilog (console em Development; JSON compacto em Production)
- **Documentação:** Swagger / OpenAPI (Swashbuckle 6.8.1) — habilitado em Development
- **Testes:** xUnit + FluentAssertions (368 testes: 215 de integração + 153 unitários)
- **Cobertura:** Coverlet; o CI exige no mínimo 80%
- **Padrões:** Repository Pattern, DTOs, Dependency Injection
- **CI/CD:** GitHub Actions (build, testes, cobertura) e publicação da imagem Docker no GHCR

## ✨ Funcionalidades Implementadas

### 🐶 Gestão de Pets

#### ✅ **Busca de Pets (GET /api/pets/search)**
- Sistema completo de busca com múltiplos filtros:
  - **Localização:** Estado e cidade do dono (texto; sem filtro por km/lat-lng)
  - **Características:** Espécie, raça, gênero, porte, faixa de idade
  - **Atributos:** Cor, pelagem e padrão (tags)
  - **Período:** Data de publicação (hoje, última semana, último mês)
- Faixas de idade: `Baby` (0–11 meses), `Young` (12–36), `Adult` (36–96). Pets com mais de 8 anos ficam de fora quando o filtro `age` é usado
- Paginação com metadados (`page`, `pageSize`, `totalCount`, `totalPages`)
- Ordenação por data de criação (mais recentes primeiro)
- Exclusão automática de pets já adotados
- Query splitting otimizado para performance

#### ✅ **Detalhes do Pet (GET /api/pets/{id})**
- Retorna informações completas do pet (espécie, raça, imagens, tags)
- O dono vem como perfil **público** (`PublicUserResponseDto`: nome, foto, cidade/estado, tipo de conta, descrição, CNPJ). Email, telefone e endereço não são expostos no anúncio
- Carregamento otimizado com `.AsSplitQuery()`
- Suporta pets adotados (para histórico)

#### ✅ **Criação de Pet (POST /api/pets)**
- Requer autenticação JWT; o `UserId` do dono é extraído do token (`sub`), não é enviado pelo cliente
- Validação de espécie, raça (pertence à espécie) e tags
- `CreatePetDto` ainda exige `ImageUrls` (1–6 URLs). O fluxo de upload separado aceita no máximo **5** imagens por pet
- Campos opcionais: nome, idade (0 = desconhecida)
- Retorna `Location` apontando para o pet criado

#### ✅ **Favoritar Pet (POST /api/pets/{id}/favorite, DELETE /api/pets/{id}/favorite, GET /api/pets/me/favorites)**
- Usuário autenticado pode favoritar e remover favoritos
- Comportamento idempotente: favoritar o mesmo pet várias vezes não cria duplicatas
- Persistência na entidade `PetFavorite` (`UserId`, `PetId`)

#### ✅ **Upload e Deleção de Imagens**

- Cada requisição aceita **1 imagem** (`multipart/form-data`, campo `file`). Para várias fotos, chame o endpoint várias vezes (máximo de 5 por pet)
- Validações: tipo (`.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`), tamanho máximo 5MB, apenas o dono altera imagens
- Transação no banco; se o Cloudinary falhar depois do insert, o repositório faz rollback e tenta limpar o arquivo (best-effort)

Endpoints:
- `POST /api/pets/{petId}/images` — upload (autenticado)
- `GET /api/pets/{petId}/images` — lista imagens (público)
- `DELETE /api/pets/{petId}/images/{imageId}` — deleta (autenticado, dono)

As chaves `ApiKey` e `ApiSecret` devem vir de variáveis de ambiente. Veja `.env.example`.

**🧪 Exemplo de Upload (cURL):**
```bash
curl -X POST "http://localhost:5096/api/pets/1/images" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@/path/to/image.jpg"
```

### 📚 Catálogo (species, breeds, tags)

Lookups públicos para montar formulários e filtros — não é preciso usar IDs mágicos do seeder.

- `GET /api/species` — espécies (Dog, Cat, Bird, Rabbit)
- `GET /api/species/{id}/breeds` — raças da espécie
- `GET /api/tags?category=Color|Pattern|Coat` — tags, com filtro opcional por categoria

### 👤 Gestão de Utilizadores & Autenticação

#### ✅ **Autenticação JWT (POST /api/auth/register & /api/auth/login)**
- Registro com hash BCrypt
- Conta `Person` ou `Shelter` (`UserType`); abrigo exige CNPJ válido
- Login com validação de credenciais
- JWT Bearer com expiração configurável (padrão: 60 minutos)
- Claims: `sub` (userId), email, `jti`
- UUID v7 para IDs de usuário
- **Options Pattern** + validação on startup
- Clock skew padrão (5 minutos)

#### Uso do claim `sub` como `Name`

O projeto mapeia o claim JWT `sub` para `User.Identity.Name`:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
  // ... outras configurações ...
  NameClaimType = JwtRegisteredClaimNames.Sub,
};
```

Não há claim separado `ClaimTypes.NameIdentifier` no token. Clientes devem usar `User.Identity.Name` ou o claim `sub`.

#### ✅ **Ciclo de vida da conta**
- Verificação de email após o registro (`POST /api/auth/verify-email`, `POST /api/auth/resend-verification`)
- Recuperação de senha (`POST /api/auth/forgot-password`, `POST /api/auth/reset-password`)
- Sem SMTP configurado, os e-mails são apenas logados (útil em Development)

#### ✅ **Perfis**
- `GET /api/users/me` — perfil privado do autenticado (email, telefone, endereço)
- `GET /api/users/{id}` — perfil público sanitizado (anônimo)
- `PATCH /api/users/me` / `DELETE /api/users/me` — atualizar ou apagar a própria conta
- `User.ProfilePictureUrl` existe no modelo e nos DTOs; ainda **não há** upload de foto de perfil nem campo correspondente no `PatchUserDto`

#### ✅ **Autorização**
- Endpoints mutáveis de pets, adoção, chat, notificações e `/me` usam `[Authorize]`
- O `UserId` é extraído do JWT no servidor

## 💬 Chat, Adoção e Notificações

### Chat persistido

Conversas e mensagens são gravadas no banco (`Conversation`, `ChatMessage`). O hub exige autenticação.

- REST inbox/histórico: `GET/POST /api/conversations` (criar pelo `PetId` ou `AdoptionRequestId`)
- Mensagens: `GET/POST /api/conversations/{id}/messages`, `POST /api/conversations/{id}/read`
- SignalR em **`/chatHub`** (não `/hubs/chat`). Token JWT na query `access_token` no handshake (WebSockets não enviam header `Authorization`)
- Métodos do hub: `JoinChat`, `SendMessage`, `MarkAsRead`
- Eventos: `ReceiveMessage`, `MessagesRead`
- O remetente vem do JWT; o cliente não escolhe `senderName` nem finge outro usuário

### Pedidos de adoção

- Criar, listar (enviados/recebidos/por pet), aprovar, rejeitar, cancelar (adotante) e marcar adotado fora da plataforma
- `POST /api/adoption-requests/{id}/approve` marca o pet como adotado e rejeita os demais pedidos pendentes
- `PATCH /api/adoption-requests/{id}/status` só altera aquele pedido (não transfere o pet nem rejeita os outros)
- O `UserId` do pet **não muda** após a aprovação (o anunciante continua dono no banco)

### Notificações in-app

Eventos de adoção (`created`, `approved`, `rejected`, `cancelled`) geram notificação persistida e push no hub **`/notificationHub`** (JWT via `access_token`).

- `GET /api/notifications`, `GET /api/notifications/unread-count`
- `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`
- Evento SignalR: `ReceiveNotification`

## 🧪 Testes

O projeto tem **368 testes** (215 de integração + 153 unitários). A suíte cobre pets, catálogo, auth (incluindo refresh e ciclo de email/senha), usuários, favoritos, imagens, adoção, conversas, hubs SignalR, notificações, health check e rate limiting.

```bash
# Todos os testes
dotnet test

# Apenas unitários ou integração
dotnet test --filter "FullyQualifiedName~PetHub.Tests.UnitTests"
dotnet test --filter "FullyQualifiedName~PetHub.Tests.IntegrationTests"

# Com cobertura
dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"
```

O CI falha se a cobertura de linhas ficar abaixo de **80%**.

### Boas práticas
- `TestConstants` para dados de teste
- `WebApplicationFactory` com banco isolado por teste
- FluentAssertions
- Cloudinary e SMTP mockados nos testes de integração

## 🛠️ Configuração do Ambiente

### Pré-requisitos

- ✅ [.NET 8 SDK](https://dotnet.microsoft.com/download)
- ✅ Banco MySQL (recomendado: [TiDB Cloud Serverless](https://tidbcloud.com/))
- ✅ Git
- 📦 Editor: Visual Studio Code ou Visual Studio 2022+

### 1. Clonar o Repositório

```bash
git clone https://github.com/christianbvolz/PetHub-Backend.git
cd PetHub-Backend
```

### 2. Configurar Variáveis de Ambiente

Copie `.env.example` para `.env` **na raiz do repositório** (mesmo nível que `pethub.sln`):

```env
DB_CONNECTION_STRING="Server=gateway01.us-east-1.prod.aws.tidbcloud.com;Port=4000;Database=test;Uid=SEU_USUARIO;Pwd=SUA_SENHA;SslMode=VerifyCA;"
FRONTEND_URL="http://localhost:3000;http://localhost:5173"
JWT_SECRET="minha_chave_secreta_super_segura_pethub_2025"

CLOUDINARY_CLOUD_NAME=yourCloudName
CLOUDINARY_API_KEY=yourApiKey
CLOUDINARY_API_SECRET=yourApiSecret

# Deixe SMTP_HOST vazio em local para só logar os e-mails
SMTP_HOST=""
SMTP_PORT="587"
SMTP_USER=""
SMTP_PASSWORD=""
SMTP_FROM="noreply@pethub.local"
SMTP_FROM_NAME="PetHub"
```

> **⚠️ Importante:** `.env` está no `.gitignore`. Nunca commite credenciais.

O cookie `refreshToken` é sempre `Secure=true`. Em HTTP puro (`http://localhost:5096`) o navegador pode recusar o cookie; use o perfil HTTPS (`https://localhost:7059`) ou envie o refresh token no body (aceito para testes).

### 3. Instalar Dependências

```bash
dotnet restore
dotnet tool install --global dotnet-ef
```

### 4. Configurar o Banco de Dados

Na subida da API, `DatabaseInitializer` aplica as migrations EF (`MigrateAsync`) e o catálogo (espécies, raças, tags). Dados de demonstração (usuários e pets) **só são inseridos em Development**, se o banco ainda não tiver users/pets.

```bash
cd src/PetHub.API
dotnet ef database update
```

Seed de catálogo (qualquer ambiente, se as tabelas estiverem vazias):
- Espécies: Dog, Cat, Bird, Rabbit
- Raças de cães e gatos (Labrador, Poodle, Siamese, Persian, etc.)
- Tags de cor, padrão e pelagem

Seed de demo (somente Development):
- 3 pessoas + 1 abrigo (`Patas Amigas`)
- 50 pets de exemplo

## ▶️ Como Rodar

### Modo Desenvolvimento

```bash
cd src/PetHub.API
dotnet watch run
```

Ou pressione **F5**. Perfis em `Properties/launchSettings.json`: `http` (porta **5096**) e `https` (7059 + 5096).

### Acessar a Aplicação

- 📘 **Swagger:** http://localhost:5096/swagger (somente `Development`)
- 🌐 **API Base:** http://localhost:5096/api
- 📊 **Health Check:** http://localhost:5096/health (anônimo; verifica o banco)

### Docker

Há um `Dockerfile` na raiz (contexto do build = repositório). A imagem escuta na porta **8080** e o healthcheck chama `/health`.

```bash
docker build -t pethub-api .
docker run --rm -p 8080:8080 \
  -e JWT_SECRET="minha_chave_secreta_super_segura_pethub_2025" \
  -e DB_CONNECTION_STRING="Server=...;Port=4000;Database=test;Uid=...;Pwd=...;SslMode=VerifyCA;" \
  -e FRONTEND_URL="http://localhost:3000" \
  pethub-api
```

## 📂 Estrutura do Projeto

```
PetHub-Backend/
├── src/
│   └── PetHub.API/              # Único projeto da API
│       ├── Controllers/          # Auth, Pets, Users, Species, Tags, AdoptionRequests,
│       │                         # Conversations, Notifications
│       ├── Models/               # Pet, User, Conversation, ChatMessage, Notification, etc.
│       ├── DTOs/                 # Pet, User, Catalog, Chat, AdoptionRequest, Notification
│       ├── Services/             # Repositórios + ChatService, AdoptionService, NotificationService
│       ├── Configuration/        # Jwt, RefreshToken, SMTP, RateLimiting, AuthLifecycle
│       ├── Mappings/             # Entidade → DTO
│       ├── Data/                 # AppDbContext, Migrations, DbSeeder, DatabaseInitializer
│       ├── Enums/
│       ├── Hubs/                 # ChatHub (/chatHub), NotificationHub (/notificationHub)
│       ├── Middlewares/          # GlobalExceptionMiddleware
│       ├── Utils/
│       └── Program.cs
│
├── tests/
│   └── PetHub.Tests/
│       ├── IntegrationTests/     # Controllers, Hubs, Health, RateLimiting
│       └── UnitTests/            # Services, mappings, utils
│
├── .github/workflows/            # ci.yml, cd.yml, pr-checks.yml, codeql.yml
├── Dockerfile
├── .env.example
├── pethub.sln
└── README.md
```

### 📐 Arquitetura

O código vive em **um projeto** (`PetHub.API`): controllers, entidades, repositórios e regras juntos. Há Repository Pattern, DTOs e DI — não um split em camadas de Clean Architecture.

- **Controllers:** HTTP + autorização; regras mais pesadas ficam em serviços (`AdoptionService`, `ChatService`, `NotificationService`, `AuthLifecycleService`)
- **DTOs públicos vs privados:** anúncios e `GET /api/users/{id}` usam `PublicUserResponseDto`; `/api/users/me` usa `UserResponseDto`
- **Middleware:** exceções globais com resposta padronizada
- **Rate limiting:** 100 req/IP/minuto no global; política `auth` com 10 req/IP/minuto em `/api/auth/*`. `/health` fica de fora

### 🗄️ Modelo de Dados (Principais Entidades)

```
User (Person | Shelter)
├── Pets[]
├── RefreshTokens[]
├── AuthTokens[]          (verificação de email / reset de senha)
└── (favoritos via PetFavorite, não navegação na entidade)

Pet
├── Species / Breed
├── User (anunciante; não muda após aprovação)
├── Images[]
├── Tags[] (via PetTag)
└── AdoptionRequests[]

Conversation
├── UserA / UserB
├── Pet? / AdoptionRequest?
└── Messages[] (ChatMessage)

Notification (por usuário, eventos de adoção)
Species → Breeds[]
Tag (Color, Pattern, Coat)
```

## 🔄 Workflow de Desenvolvimento

### Branches

- `main` - produção
- `feat/*` - funcionalidades
- `fix/*` - correções
- `docs/*` - documentação

### CI/CD

- GitHub Actions: restore, build, testes, cobertura (≥ 80%), scan de pacotes vulneráveis
- PRs: título Conventional Commits (`pr-checks.yml`)
- Push em `main`: `cd.yml` publica a imagem em `ghcr.io/<org>/PetHub-Backend`

## 🚢 Deploy (Produção)

A imagem Docker é construída na raiz do repositório e enviada ao GitHub Container Registry. O passo de deploy em servidor no `cd.yml` está comentado (exemplo).

Variáveis necessárias:

```env
DB_CONNECTION_STRING=Server=xxx;Port=4000;Database=test;Uid=xxx;Pwd=xxx;SslMode=VerifyCA;
FRONTEND_URL=https://seu-frontend.vercel.app
JWT_SECRET=sua_chave_secreta_super_longa
ASPNETCORE_ENVIRONMENT=Production
CLOUDINARY_CLOUD_NAME=...
CLOUDINARY_API_KEY=...
CLOUDINARY_API_SECRET=...
SMTP_HOST=...
SMTP_PORT=587
SMTP_USER=...
SMTP_PASSWORD=...
SMTP_FROM=noreply@seu-dominio.com
```

Em produção o Cloudinary é validado no startup. Sem `SMTP_HOST`, os e-mails de verificação/reset só são logados.

## 📚 Endpoints da API

### 🐶 Pets

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| `GET` | `/api/pets/search` | Buscar pets com filtros | Público |
| `GET` | `/api/pets/{id}` | Detalhes (dono público) | Público |
| `GET` | `/api/pets/me` | Pets do usuário | Sim |
| `POST` | `/api/pets` | Criar pet (owner = JWT) | Sim |
| `PATCH` | `/api/pets/{id}` | Atualizar pet | Sim (dono) |
| `DELETE` | `/api/pets/{id}` | Remover pet | Sim (dono) |
| `POST` | `/api/pets/{id}/favorite` | Favoritar | Sim |
| `DELETE` | `/api/pets/{id}/favorite` | Remover favorito | Sim |
| `GET` | `/api/pets/me/favorites` | Listar favoritos | Sim |
| `POST` | `/api/pets/{petId}/images` | Upload de 1 imagem | Sim (dono) |
| `GET` | `/api/pets/{petId}/images` | Listar imagens | Público |
| `DELETE` | `/api/pets/{petId}/images/{imageId}` | Deletar imagem | Sim (dono) |

### 📚 Catálogo

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| `GET` | `/api/species` | Listar espécies | Público |
| `GET` | `/api/species/{id}/breeds` | Raças da espécie | Público |
| `GET` | `/api/tags` | Tags (`?category=Color\|Pattern\|Coat`) | Público |

### 🔐 Autenticação

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/auth/register` | Registrar (JWT + cookie refresh) |
| `POST` | `/api/auth/login` | Login |
| `POST` | `/api/auth/refresh` | Renovar access token |
| `POST` | `/api/auth/revoke` | Logout / revogar refresh |
| `POST` | `/api/auth/verify-email` | Confirmar e-mail |
| `POST` | `/api/auth/resend-verification` | Reenviar verificação |
| `POST` | `/api/auth/forgot-password` | Pedir reset de senha |
| `POST` | `/api/auth/reset-password` | Definir nova senha |

### 👤 Usuários

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| `GET` | `/api/users/me` | Perfil privado | Sim |
| `GET` | `/api/users/{id}` | Perfil público | Público |
| `PATCH` | `/api/users/me` | Atualizar a própria conta | Sim |
| `DELETE` | `/api/users/me` | Apagar a própria conta | Sim |

### 💬 Chat

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/conversations` | Criar/obter conversa (`PetId` ou `AdoptionRequestId`) |
| `GET` | `/api/conversations` | Inbox |
| `GET` | `/api/conversations/{id}` | Detalhe |
| `GET` | `/api/conversations/{id}/messages` | Histórico (`beforeId`, `pageSize`) |
| `POST` | `/api/conversations/{id}/messages` | Enviar (também notifica o hub) |
| `POST` | `/api/conversations/{id}/read` | Marcar como lido |
| SignalR | `/chatHub` | Tempo real (JWT em `?access_token=`) |

### 🐾 Adoção

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/adoption-requests` | Criar pedido |
| `GET` | `/api/adoption-requests/{id}` | Detalhe (adotante ou dono) |
| `GET` | `/api/adoption-requests/me/sent` | Enviados |
| `GET` | `/api/adoption-requests/me/received` | Recebidos |
| `GET` | `/api/adoption-requests/pet/{petId}` | Pedidos do pet (dono) |
| `GET` | `/api/adoption-requests/pet/{petId}/pending` | Pendentes do pet (dono) |
| `PATCH` | `/api/adoption-requests/{id}/status` | Atualizar status (não marca o pet) |
| `POST` | `/api/adoption-requests/{id}/approve` | Aprovar, marcar adotado, rejeitar os outros |
| `POST` | `/api/adoption-requests/{id}/cancel` | Cancelar (adotante) |
| `POST` | `/api/adoption-requests/pet/{petId}/mark-adopted` | Adotado fora da plataforma |

### 🔔 Notificações

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/notifications` | Lista in-app |
| `GET` | `/api/notifications/unread-count` | Contagem não lidas |
| `POST` | `/api/notifications/{id}/read` | Marcar uma |
| `POST` | `/api/notifications/read-all` | Marcar todas |
| SignalR | `/notificationHub` | Push (`ReceiveNotification`) |

### 📊 Operação

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/health` | Liveness + banco (anônimo, sem rate limit) |

## 🎯 Próximos Passos

### Backend (API)

- [x] Autenticação JWT + refresh tokens
- [x] Pedidos de adoção (incluindo cancelar)
- [x] Favoritos e upload Cloudinary
- [x] Catálogo (species / breeds / tags)
- [x] Chat persistido + auth no hub
- [x] Perfil público vs privado (sem PII no anúncio)
- [x] Pessoa vs abrigo (`UserType`, CNPJ, descrição)
- [x] Notificações in-app (SignalR)
- [x] Verificar e-mail e recuperar senha
- [x] Dockerfile, health check, migrations EF, Serilog, rate limiting
- [ ] Filtro geográfico por proximidade (lat/lng) — estado/cidade já existem
- [ ] Faixa de idade **Senior** (hoje o filtro `Adult` para em 96 meses)
- [ ] Upload de foto de perfil
- [ ] Unificar limite de imagens (DTO de criação 1–6 vs upload máximo 5)
- [ ] Cache (Redis)
- [ ] Headers de cache / endpoint `/meta` para Open Graph (SSR)

Itens que podem esperar: auditoria de IP, nome de dispositivo na sessão, split em projetos de Clean Architecture.

### 🔐 Refresh Tokens (JWT)

O backend usa refresh tokens rotativos no cookie `HttpOnly` para reduzir XSS.

Principais pontos
- Cookie: `refreshToken` (HttpOnly, `Secure`, `SameSite=Lax`) com expiração de 14 dias
- Rotação: `/api/auth/refresh` revoga o token atual e emite outro
- Só o hash SHA-256 é persistido
- Reuso de token revogado/expirado revoga todas as sessões do usuário
- `POST /api/auth/revoke` invalida o token (logout)
- `RefreshTokenCleanupService` remove expirados a cada 1 hora

Endpoints
- `POST /api/auth/refresh` — lê o cookie `refreshToken`; fallback JSON `{ "refreshToken": "..." }` para testes
- `POST /api/auth/revoke` — cookie ou body; apaga o cookie no cliente

```bash
curl -i -X POST https://localhost:7059/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'

curl -i -X POST https://localhost:7059/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<TOKEN_OBTIDO_PELO_COOKIE>"}'
```

Segurança
- HTTPS em produção: o flag `Secure` impede envio em HTTP
- Cookies HttpOnly contra XSS
- Tokens rotativos e detecção de reutilização
- Sem armazenamento de IPs (LGPD/GDPR)

### Melhorias de Segurança (opcionais)

#### Auditoria de IP
Não coletamos IPs por padrão. Se necessário: modelo `RefreshTokenAudit` com retenção limitada e base legal.

#### Identificação de sessões
Nome amigável do device / User-Agent truncado para o usuário revogar sessões específicas. Campos sugeridos: `DeviceName`, `UserAgentInfo` (opt-in via `RefreshTokenSettings.CollectDeviceInfo`).

### Melhorias para SSR (Server-Side Rendering)

- [ ] **Cache Headers** em GETs públicos (`Cache-Control`, `ETag`)
- [ ] **`/api/pets/{id}/meta`** para Open Graph
- [ ] CORS com `Access-Control-Max-Age` para `getServerSideProps`
- [ ] Rate limiting diferenciado SSR vs CSR

### Frontend (Futuro)

Estrutura de renderização híbrida planejada com **Next.js 14+**:

#### 🎨 Arquitetura de Renderização

**SSR** para páginas públicas, listagem e detalhes do pet.

**CSR** para dashboard, chat (SignalR), favoritos, formulários e painel.

**Benefícios:** SEO, Open Graph, performance nas páginas públicas e interatividade no dashboard.

#### 🔗 Estrutura de URLs Híbrida

```
/pets                          → Lista (SSR)
/pets/cachorro                 → Espécie (SSR)
/pets/[id]                     → Detalhes (SSR)
/pets/cachorro?breed=labrador&age=young&size=large
```

**Exemplo Next.js:**
```javascript
// app/pets/[species]/[city]/page.tsx
export async function generateMetadata({ params }) {
  return {
    title: `Adote um ${params.species} em ${params.city} - PetHub`,
    description: `Encontre ${params.species}s para adoção em ${params.city}`,
    openGraph: {
      title: `${params.species} para adoção em ${params.city}`,
      images: ['/og-image-pets.jpg'],
    }
  }
}

export default async function PetsPage({ params, searchParams }) {
  const pets = await fetch(
    `${API_URL}/api/pets/search?species=${params.species}&city=${params.city}&age=${searchParams.age || ''}`
  )

  return <PetList pets={pets} />
}
```

#### 📱 Stack Tecnológica Recomendada

- **Framework:** Next.js 14+ (App Router)
- **Estilo:** Tailwind CSS + shadcn/ui
- **State:** Zustand + React Query
- **Realtime:** SignalR Client (`/chatHub`, `/notificationHub`)
- **Forms:** React Hook Form + Zod
- **Auth:** NextAuth.js v5 (JWT)

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch (`git checkout -b feat/nova-funcionalidade`)
3. Commit (`git commit -m 'feat: adiciona nova funcionalidade'`)
4. Push (`git push origin feat/nova-funcionalidade`)
5. Abra um Pull Request

### Padrão de Commits

[Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` Nova funcionalidade
- `fix:` Correção de bug
- `docs:` Documentação
- `test:` Testes
- `refactor:` Refatoração
- `chore:` Manutenção

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE).

## 👤 Autor

**Christian Berny Volz**

- GitHub: [@christianbvolz](https://github.com/christianbvolz)
- LinkedIn: [Christian Berny Volz](https://www.linkedin.com/in/christian-berny-volz/)

---

<div align="center">
  Desenvolvido com 💜
  <br>
  <sub>Ajudando pets a encontrarem um lar 🐾</sub>
</div>
