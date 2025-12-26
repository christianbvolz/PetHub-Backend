# 🐾 PetHub - Backend API

O PetHub é uma plataforma que conecta pessoas que desejam adotar animais de estimação com donos ou abrigos que possuem animais para adoção. Este repositório contém o Backend (API) da aplicação, construído com tecnologias modernas do ecossistema .NET.

[![CI](https://github.com/christianbvolz/PetHub-Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/christianbvolz/PetHub-Backend/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-203%20passing-brightgreen)](tests/)
[![Coverage](https://img.shields.io/badge/coverage-87.8%25-brightgreen)](tests/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 🚀 Tecnologias Utilizadas

- **Linguagem:** C# (.NET 8)
- **Framework:** ASP.NET Core Web API (Minimal APIs)
- **Banco de Dados:** MySQL (Hospedado no TiDB Cloud Serverless)
- **ORM:** Entity Framework Core 8 (Pomelo MySQL Provider)
- **Tempo Real:** SignalR (Para o sistema de Chat)
- **Segurança:** BCrypt (Hash de senhas)
- **Documentação:** Swagger / OpenAPI (Swashbuckle 6.8.1)
- **Testes:** xUnit + FluentAssertions (203 testes: 178 integration + 25 unit tests)
- **Cobertura:** 87.8% de cobertura de código (Coverlet)
- **Padrões:** Repository Pattern, DTOs, Dependency Injection
- **CI/CD:** GitHub Actions com verificação de cobertura

## ✨ Funcionalidades Implementadas

### 🐶 Gestão de Pets

#### ✅ **Busca de Pets (GET /api/pets/search)**
- Sistema completo de busca com múltiplos filtros:
  - **Localização:** Estado, Cidade (do dono)
  - **Características:** Espécie, Raça, Gênero, Porte, Idade
  - **Atributos:** Cor, Pelagem (através de Tags)
  - **Período:** Data de publicação (hoje, última semana, último mês)
- Paginação integrada com metadados (page, pageSize, totalCount, totalPages)
- Ordenação por data de criação (mais recentes primeiro)
- Exclusão automática de pets já adotados
- Query splitting otimizado para performance

#### ✅ **Detalhes do Pet (GET /api/pets/{id})**
- Retorna informações completas do pet
- Inclui todas as relações: Dono, Espécie, Raça, Imagens, Tags
- Carregamento otimizado com `.AsSplitQuery()`
- Suporta pets adotados (para histórico)

#### ✅ **Criação de Pet (POST /api/pets)**
- Validação completa de dados:
  - Verifica se Species existe
  - Verifica se Breed pertence à Species correta
  - Valida existência de todas as Tags
- Suporte a múltiplas imagens (até 6)
- Suporte a múltiplas tags (cores, pelagem, etc)
- Campos opcionais: Nome, Idade (0 = desconhecida)
- Relacionamento automático com User (temporariamente hardcoded - userId=1)
- Retorna Location header apontando para o pet criado
 - Retorna Location header apontando para o pet criado

#### ✅ **Favoritar Pet (POST /api/pets/{id}/favorite, DELETE /api/pets/{id}/favorite, GET /api/pets/me/favorites)**
- Usuário autenticado pode favoritar e remover favoritos de pets.
- Comportamento idempotente: favoritar o mesmo pet múltiplas vezes não cria duplicatas.
- Endpoints:
  - `POST /api/pets/{id}/favorite` — adiciona o pet aos favoritos do usuário autenticado.
  - `DELETE /api/pets/{id}/favorite` — remove o pet dos favoritos do usuário autenticado.
  - `GET /api/pets/me/favorites` — lista os pets favoritados pelo usuário.
- Implementação:
  - Métodos do repositório: `AddFavoriteAsync`, `RemoveFavoriteAsync`, `GetUserFavoritePetsAsync`.
  - Armazenamento no banco via entidade `PetFavorite` (UserId, PetId).
  - Testes de integração adicionados para favoritar, desfavoritar e idempotência.
### 📊 Sistema de Tags
- **Categorias:** Color (Cor), Pattern (Padrão), Coat (Pelagem)
- Permite classificação flexível dos pets
- Suporte a múltiplas tags por pet
- Filtros AND/OR configuráveis

### 👤 Gestão de Utilizadores & Autenticação

#### ✅ **Autenticação JWT (POST /api/auth/register & /api/auth/login)**
- Registro seguro com hash BCrypt (12 rounds)
- Login com validação de credenciais
- Geração de tokens JWT (Bearer authentication)
- Tokens com expiração configurável (padrão: 60 minutos)
- Claims customizados: userId, email, sub, jti
- UUID v7 para IDs de usuário (segurança contra enumeração)
- **Options Pattern** para configuração fortemente tipada
- **Validação on Startup** com Data Annotations
- Clock Skew configurado (tolerância de 5 minutos)

#### Uso do claim `sub` como `Name` (configuração de validação JWT)

O projeto mapeia o claim JWT padrão `sub` (subject) para o claim de nome usado pelo runtime (`User.Identity.Name`).
Isso evita emitir claims duplicados (por exemplo `sub` e `ClaimTypes.NameIdentifier`) e faz com que bibliotecas que leem `User.Identity.Name` retornem diretamente o id do usuário.

Trecho-chave (em `Program.cs`):

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
  // ... outras configurações ...
  NameClaimType = JwtRegisteredClaimNames.Sub,
};
```

Compatibilidade: removemos a emissão separada de `ClaimTypes.NameIdentifier` no token. Se algum client/integração depender desse claim, atualize para usar `User.Identity.Name` ou leia o claim `sub` diretamente do token.

#### ✅ **Autorização**
- Endpoints protegidos com `[Authorize]`
- Extração automática do UserId do token JWT
- POST /api/pets requer autenticação
- Middleware de autenticação configurado globalmente

## 💬 Comunicação & Adoção (Estrutura Base)

- **Chat em Tempo Real:** SignalR configurado
- **Pedidos de Adoção:** Modelo de dados pronto
- **Favoritos:** Implementado — endpoints para favoritar, desfavoritar e listar favoritos por usuário; métodos do repositório `AddFavoriteAsync`, `RemoveFavoriteAsync`, `GetUserFavoritePetsAsync` e testes de integração adicionados.
## 🧪 Testes

O projeto possui uma suite completa de **43 testes de integração** com 100% de aprovação:

- **GetPet:** 11 testes (validação de ID, relacionamentos, erros)
- **SearchPets:** 14 testes (filtros, paginação, ordenação)
- **CreatePet:** 18 testes (validações, relacionamentos, **autenticação JWT**, autorização)

```bash
# Executar todos os testes
dotnet test

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~GetPetIntegrationTests"
dotnet test --filter "FullyQualifiedName~SearchPetsIntegrationTests"
dotnet test --filter "FullyQualifiedName~CreatePetIntegrationTests"

# Executar com detalhes
dotnet test --logger "console;verbosity=detailed"
```

### Cobertura de Testes
- ✅ Cenários de sucesso
- ✅ Validações de entidades relacionadas
- ✅ Casos de erro (404, 400, 500)
- ✅ Campos opcionais e valores padrão
- ✅ Integridade dos dados e relacionamentos
- ✅ Preparação para autenticação (TODO)

## 🛠️ Configuração do Ambiente

### Pré-requisitos

- ✅ [.NET 8 SDK](https://dotnet.microsoft.com/download) instalado
- ✅ Acesso a um banco MySQL (Recomendado: [TiDB Cloud Serverless](https://tidbcloud.com/) - tier gratuito)
- ✅ Git
- 📦 Editor: Visual Studio Code ou Visual Studio 2022+

### 1. Clonar o Repositório

```bash
git clone https://github.com/christianbvolz/PetHub-Backend.git
cd PetHub-Backend
```

### 2. Configurar Variáveis de Ambiente

Crie um arquivo `.env` **na raiz do projeto** (mesmo nível que `PetHub-Backend.sln`):

```env
# Conexão com o Banco de Dados (TiDB / MySQL)
DB_CONNECTION_STRING="Server=gateway01.us-east-1.prod.aws.tidbcloud.com;Port=4000;Database=test;Uid=SEU_USUARIO;Pwd=SUA_SENHA;SslMode=VerifyCA;"

# URLs permitidas (CORS) - separe por ponto e vírgula
FRONTEND_URL="http://localhost:3000;http://localhost:5173"

# Chave secreta para JWT (use uma string aleatória longa)
JWT_SECRET="minha_chave_secreta_super_segura_pethub_2025"
```

> **⚠️ Importante:** O arquivo `.env` está no `.gitignore` por segurança. Nunca commit credenciais!

### 3. Instalar Dependências

```bash
# Restaurar pacotes NuGet
dotnet restore

# Instalar ferramenta de migrations (se necessário)
dotnet tool install --global dotnet-ef
```

### 4. Configurar o Banco de Dados

```bash
# Aplicar migrations (criar tabelas)
cd src/PetHub.API
dotnet ef database update

# Verificar se o seeding foi executado
# A aplicação popula automaticamente dados iniciais na primeira execução
```

O banco será populado com:
- Espécies: Cachorro, Gato
- Raças: Labrador, Poodle, Siamês, Persa
- Tags: Branco, Preto, Marrom (cores) + Curto, Longo (pelagem)
- 1 usuário de teste
- 6 pets de exemplo (5 disponíveis + 1 adotado)

## ▶️ Como Rodar

### Modo Desenvolvimento

```bash
# Navegar para o projeto da API
cd src/PetHub.API

# Iniciar servidor de desenvolvimento com hot reload
dotnet watch run
```

Ou pressione **F5** no Visual Studio / VS Code.

### Acessar a Aplicação

- 📘 **Swagger (Documentação):** http://localhost:5096/swagger
- 🌐 **API Base:** http://localhost:5096/api
- 📊 **Health Check:** http://localhost:5096/health (quando implementado)

> **Nota:** A porta padrão é `5096`. Se estiver diferente, verifique o terminal ou `Properties/launchSettings.json`.

### Executar Testes

```bash
# Voltar para a raiz do projeto
cd ../..

# Executar todos os testes
dotnet test

# Ver detalhes dos testes
dotnet test --logger "console;verbosity=detailed"

# Executar com cobertura (requer ferramenta adicional)
dotnet test --collect:"XPlat Code Coverage"
```

## 📂 Estrutura do Projeto

```
PetHub-Backend/
├── src/
│   └── PetHub.API/              # Projeto principal da API
│       ├── Controllers/          # Endpoints HTTP (AuthController, PetsController, UsersController)
│       ├── Models/               # Entidades do banco (Pet, User, Species, RefreshToken, etc)
│       ├── DTOs/                 # Data Transfer Objects
│       │   ├── Pet/              # CreatePetDto, PetResponseDto, SearchPetsQuery
│       │   ├── User/             # LoginDto, RefreshRequestDto, RevokeRequestDto, UserResponseDto
│       │   └── Common/           # PagedResult<T>
│       ├── Services/             # Lógica de negócio
│       │   ├── IPetRepository.cs         # Interface do repositório de Pets
│       │   ├── PetRepository.cs          # Implementação com EF Core
│       │   ├── IUserRepository.cs        # Interface do repositório de Users
│       │   ├── UserRepository.cs         # Implementação com autenticação
│       │   ├── IJwtService.cs            # Interface do serviço JWT
│       │   ├── JwtService.cs             # Geração e validação de tokens JWT
│       │   ├── IRefreshTokenService.cs   # Interface do serviço de Refresh Tokens
│       │   ├── RefreshTokenService.cs    # Implementação: create, rotate, revoke
│       │   └── RefreshTokenCleanupService.cs # Background service para limpeza
│       ├── Configuration/        # Modelos de configuração (JwtSettings, RefreshTokenSettings)
│       ├── Mappings/             # Extension methods para mapear entidades → DTOs
│       ├── Data/                 # Contexto EF Core + Migrations + Seeding
│       ├── Enums/                # PetGender, PetSize, TagCategory, etc
│       ├── Hubs/                 # SignalR hubs (Chat em tempo real)
│       ├── Middlewares/          # GlobalExceptionMiddleware
│       ├── Utils/                # PasswordHelper, RefreshTokenHelper, CookieOptionsProvider
│       └── Program.cs            # Entry point + configuração + JWT auth
│
├── tests/
│   └── PetHub.Tests/            # Testes (xUnit + FluentAssertions)
│       ├── IntegrationTests/
│       │   ├── Controllers/
│       │   │   ├── AuthControllerTests/
│       │   │   │   └── RefreshTokenTests.cs  # 11 testes de refresh token
│       │   │   ├── PetsControllerTests/      # Testes de CRUD de pets
│       │   │   └── UsersControllerTests/     # Testes de usuários
│       │   ├── TestConstants.cs              # Constantes centralizadas
│       │   ├── TestDataSeeder.cs             # Dados de teste
│       │   └── PetHubWebApplicationFactory.cs # Factory para testes
│       └── UnitTests/            # Testes unitários (PasswordHelper, etc)
│       └── IntegrationTests/
│           ├── GetPetIntegrationTests.cs        # 11 testes
│           ├── SearchPetsIntegrationTests.cs    # 14 testes
│           ├── CreatePetIntegrationTests.cs     # 18 testes (com autenticação)
│           ├── AuthenticationHelper.cs          # Helper para JWT nos testes
│           ├── TestDataSeeder.cs                # Dados de teste
│           └── PetHubWebApplicationFactory.cs   # Factory para testes
│
├── .github/
│   └── workflows/
│       └── ci.yml               # GitHub Actions (CI)
│
├── .env                         # Variáveis de ambiente (não commitado)
├── .gitignore                   # Arquivos ignorados
├── PetHub-Backend.sln           # Solution do Visual Studio
└── README.md                    # Este arquivo
```

### 📐 Arquitetura

O projeto segue princípios de **Clean Architecture** e **SOLID**:

- **Controllers:** Camada fina que apenas recebe requests HTTP e delega ao repositório
- **Repository Pattern:** Abstração do acesso a dados (facilita testes e manutenção)
- **DTOs:** Separação clara entre entidades do banco e objetos de API
- **Dependency Injection:** Todas as dependências são injetadas via DI container do ASP.NET
- **Middleware:** Tratamento global de exceções com mensagens padronizadas

### 🗄️ Modelo de Dados (Principais Entidades)

```
User (Usuário/Dono)
├── Pets[] (seus pets para adoção)
├── SentMessages[] (mensagens de chat enviadas)
├── ReceivedMessages[] (mensagens recebidas)
└── FavoritePets[] (pets favoritados)

Pet (Animal para adoção)
├── Species (Espécie: Cachorro, Gato)
├── Breed (Raça: Labrador, Siamês, etc)
├── User (Dono)
├── Images[] (múltiplas fotos)
├── Tags[] (cores, pelagem, temperamento)
└── AdoptionRequests[] (pedidos de adoção)

Species → Breeds[] (1:N - uma espécie tem várias raças)
Tag (categoria: Color, Pattern, Coat)
```

## 🔄 Workflow de Desenvolvimento

### Branches

- `main` - Branch principal (produção)
- `feat/*` - Novas funcionalidades
- `fix/*` - Correções de bugs
- `docs/*` - Documentação

### CI/CD

- ✅ **GitHub Actions** configurado
- ✅ Build automático em cada push
- ✅ Testes executados automaticamente
- ✅ Validação de código

## 🚢 Deploy (Produção)

O projeto está preparado para deploy em **Render** via Docker:

1. **Dockerfile** otimizado para produção
2. **Program.cs** lê variáveis de ambiente (`DB_CONNECTION_STRING`, `FRONTEND_URL`, `JWT_SECRET`)
3. **HTTPS** automático via Render
4. **Health checks** prontos para implementar

### Variáveis de Ambiente no Render

```env
DB_CONNECTION_STRING=Server=xxx;Port=4000;Database=test;Uid=xxx;Pwd=xxx;SslMode=VerifyCA;
FRONTEND_URL=https://seu-frontend.vercel.app
JWT_SECRET=sua_chave_secreta_super_longa
ASPNETCORE_ENVIRONMENT=Production
```

## 📚 Endpoints da API

### 🐶 Pets

| Método | Endpoint | Descrição | Status |
|--------|----------|-----------|--------|
| `GET` | `/api/pets/search` | Buscar pets com filtros | ✅ Implementado |
| `GET` | `/api/pets/{id}` | Detalhes de um pet | ✅ Implementado |
| `GET` | `/api/pets/me` | Listar pets do usuário | ✅ Implementado |
| `POST` | `/api/pets` | Criar novo pet | ✅ Implementado |
| `PATH` | `/api/pets/{id}` | Atualizar pet | ✅ Implementado |
| `DELETE` | `/api/pets/{id}` | Remover pet | ✅ Implementado |
| `POST` | `/api/pets/{id}/favorite` | Adicionar pet aos favoritos do usuário autenticado | ✅ Implementado |
| `DELETE` | `/api/pets/{id}/favorite` | Remover favorito do usuário autenticado | ✅ Implementado |
| `GET` | `/api/pets/me/favorites` | Listar pets favoritados do usuário | ✅ Implementado |

### 🔐 Autenticação

| Método | Endpoint | Descrição | Status |
|--------|----------|-----------|--------|
| `POST` | `/api/auth/register` | Registrar novo usuário | ✅ Implementado |
| `POST` | `/api/auth/login` | Login JWT + Refresh Token (cookie HttpOnly) | ✅ Implementado |
| `POST` | `/api/auth/refresh` | Renovar access token usando refresh token | ✅ Implementado |
| `POST` | `/api/auth/revoke` | Revogar refresh token (logout) | ✅ Implementado |

### 👤 Usuários

| Método | Endpoint | Descrição | Status |
|--------|----------|-----------|--------|
| `GET` | `/api/users/{id}` | Perfil do usuário | ✅ Implementado |
| `PATCH` | `/api/users/{id}` | Atualizar perfil (parcial) | ✅ Implementado |
| `DELETE` | `/api/users/{id}` | Remover usuário | ✅ Implementado |

### 💬 Chat & Adoção

| Método | Endpoint | Descrição | Status |
|--------|----------|-----------|--------|
| `POST` | `/api/adoption-requests` | Criar pedido de adoção para um pet | ✅ Implementado |
| `GET` | `/api/adoption-requests/{id}` | Obter detalhes de um pedido (adotante ou dono) | ✅ Implementado |
| `GET` | `/api/adoption-requests/me/sent` | Listar pedidos enviados pelo usuário | ✅ Implementado |
| `GET` | `/api/adoption-requests/me/received` | Listar pedidos recebidos (pets do usuário) | ✅ Implementado |
| `GET` | `/api/adoption-requests/pet/{petId}` | Listar todos os pedidos de um pet (apenas dono) | ✅ Implementado |
| `GET` | `/api/adoption-requests/pet/{petId}/pending` | Listar pedidos pendentes de um pet (apenas dono) | ✅ Implementado |
| `PATCH` | `/api/adoption-requests/{id}/status` | Atualizar status do pedido (apenas dono) | ✅ Implementado |
| `POST` | `/api/adoption-requests/{id}/approve` | Aprovar pedido e marcar pet como adotado | ✅ Implementado |
| `POST` | `/api/adoption-requests/pet/{petId}/mark-adopted` | Marcar pet como adotado (fora da plataforma) | ✅ Implementado |
| `SignalR` | `/hubs/chat` | Chat em tempo real | 🚧 Implementado (base) |

## 🎯 Próximos Passos

### Backend (API)

- [x] **Implementar autenticação JWT** ✅
- [x] **Adicionar repository pattern para Users** ✅
- [x] **Proteger endpoints com [Authorize]** ✅
- [x] **Adicionar refresh tokens para JWT** ✅
  - ✅ Rotação automática de tokens
  - ✅ Cookies HttpOnly para transporte seguro
  - ✅ Detecção de reutilização com revogação de sessão
  - ✅ Background service para limpeza de tokens expirados
  - ✅ 11 testes de integração cobrindo todos os cenários
  - ✅ Documentação de segurança em DTOs e endpoints
- [x]  **Implementar sistema de favoritos** ✅
- [x] **Completar fluxo de pedidos de adoção** ✅
- [ ] Adicionar upload de imagens real (S3/Cloudinary)
- [ ] Implementar filtros geográficos (proximidade)
- [ ] Adicionar rate limiting
- [ ] Implementar cache (Redis)
- [ ] Adicionar logging estruturado (Serilog)
- [ ] Implementar health checks
- [ ] Adicionar testes unitários (além dos de integração)

### 🔐 Refresh Tokens (JWT)

O backend implementa um fluxo de refresh tokens para permitir a renovação segura de tokens de acesso (JWT). A implementação usa refresh tokens rotativos transportados via cookie `HttpOnly` para reduzir o risco de XSS.

Principais pontos
- Cookie: `refreshToken` (HttpOnly, `Secure`, `SameSite=Lax`) com expiração de 14 dias.
- Rotação: ao usar o endpoint `/api/auth/refresh` o refresh token atual é revogado e um novo é gerado e enviado como cookie.
- Armazenamento: apenas o hash SHA-256 do refresh token é persistido no banco; o valor em texto claro nunca é salvo.
- Reuso detectado: se um token revogado/expirado for reapresentado, todas as sessões (refresh tokens) do usuário são revogadas por segurança.
- Revogação manual: endpoint `/api/auth/revoke` permite invalidar um token (logout de uma sessão específica).
- Limpeza automática: um `BackgroundService` remove tokens expirados periodicamente.

Endpoints
- `POST /api/auth/refresh` — Renova o access token usando o refresh token. O controller lê primeiro o cookie `refreshToken`; como fallback ele aceita um body JSON `{ "refreshToken": "..." }` (útil para testes ou clients que não usam cookies).
- `POST /api/auth/revoke` — Revoga o refresh token atual (lê cookie ou body) e remove o cookie no cliente.

Exemplo (login retorna cookie HttpOnly):

```bash
# Login (o refresh token será enviado como cookie HttpOnly)
curl -i -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'
```

Exemplo (usar refresh via body — útil em testes automatizados):

```bash
curl -i -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<TOKEN_OBTIDO_PELO_COOKIE>"}'
```

Segurança e boas práticas
- **HTTPS obrigatório em produção:** O cookie `Secure` garante que o refresh token só é transmitido via HTTPS.
- **Cookies HttpOnly:** Protege contra XSS ao impedir acesso JavaScript ao token. Preferir sempre cookies em produção.
- **Tokens rotativos:** Cada refresh gera um novo token e revoga o anterior, limitando janela de ataque.
- **Detecção de reutilização:** Tentativa de usar token revogado/expirado revoga todas as sessões do usuário (indica comprometimento).
- **Hashing SHA-256:** Apenas hashes são armazenados no banco; tokens em texto claro nunca persistidos.
- **Base64 URL-safe:** Tokens gerados sem caracteres especiais (`+`, `/`, `=`), seguros para cookies e URLs.
- **Sem armazenamento de IPs:** Por padrão, não coletamos IPs dos clientes para respeitar privacidade (LGPD/GDPR).
- **Documentação de segurança:** DTOs (`RefreshRequestDto`, `RevokeRequestDto`) e endpoints contêm avisos XML sobre uso correto de cookies HttpOnly.

Operação e manutenção
- **Background Service:** `RefreshTokenCleanupService` executa limpeza automática de tokens expirados a cada 1 hora.
- **Configuração centralizada:** `RefreshTokenSettings` via Options pattern (tempo de expiração configurável).
- **Logs de auditoria:** Cada revogação registra motivo no campo `ReasonRevoked` (ex: "Rotated", "Revoked by user", "Attempted reuse").

Observações para desenvolvedores
- **Suporte dual (cookie + body):** Para facilitar testes de integração, os endpoints `/refresh` e `/revoke` aceitam token via body JSON como fallback. Em produção, preferir sempre cookies.
- **TestConstants:** Testes usam constantes centralizadas (`TestConstants`).
- **Campos do modelo `RefreshToken`:** `TokenHash`, `UserId`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenHash`, `ReasonRevoked`.
 

### Melhorias de Segurança (opcionais)

Melhorias adicionais que você pode considerar para reforçar a segurança e observabilidade das sessões:

#### Auditoria de IP (com considerações de privacidade)
- **Por que não está implementado:** Para respeitar privacidade (LGPD/GDPR), não coletamos IPs por padrão.
- **Como adicionar (se necessário):** Criar modelo `RefreshTokenAudit` separado com `IP`, `TokenId`, `Timestamp` e retenção limitada (ex: 30 dias).
- **Casos de uso válidos:** Detecção de fraude, investigação de segurança (com consentimento e base legal adequada).

#### Identificação de Sessões (melhor UX)
- **Device Name (opcional):** Cliente pode enviar nome amigável ("iPhone de João", "Chrome no Trabalho").
- **User-Agent truncado:** Armazenar apenas sistema operacional e navegador (sem versões específicas que identifiquem dispositivo único).
- **Benefício:** Usuário pode visualizar e revogar sessões específicas no painel de conta ("Encerrar sessão do iPhone").
- **Implementação sugerida:** Adicionar campos `DeviceName` e `UserAgentInfo` no modelo `RefreshToken`; tornar opcional via configuração.

#### Metadados Opcionais com Consentimento
- Criar `RefreshTokenSettings.CollectDeviceInfo` (padrão: `false`).
- Se habilitado, coletar apenas informações não-sensíveis e anonimizar após período de retenção.


### Melhorias para SSR (Server-Side Rendering)

Para suportar um frontend híbrido (SSR + CSR), algumas melhorias na API são recomendadas:

- [ ] **Cache Headers:** Configurar ResponseCache em endpoints públicos (GET /api/pets)
  - Permitir cache do lado do servidor Next.js
  - Definir TTL apropriado (ex: 60 segundos para listagens)
  - Implementar `Cache-Control`, `ETag`, `Last-Modified`

- [ ] **Endpoint de Metadados:** Criar `/api/pets/{id}/meta` para Open Graph
  - Retornar apenas título, descrição, imagem para meta tags
  - Otimizado para SSR (resposta rápida)
  - Facilitar compartilhamento em redes sociais

- [ ] **CORS Aprimorado:** Configurar headers específicos para SSR
  - Permitir `getServerSideProps` do Next.js
  - Configurar `Access-Control-Max-Age` adequado

- [ ] **Rate Limiting Diferenciado:** Limites diferentes para SSR vs CSR
  - Rotas SSR (server-to-server): limites mais generosos
  - Rotas CSR (client-to-server): limites mais restritivos
  - Implementar via AspNetCoreRateLimit com IP whitelisting

### Frontend (Futuro)

Estrutura de renderização híbrida planejada com **Next.js 14+**:

#### 🎨 Arquitetura de Renderização

**SSR (Server-Side Rendering)** para:
- 🏠 Páginas públicas (landing page, sobre)
- 🔍 Listagem de pets (`/pets`, `/pets/cachorro`, `/pets/gato`)
- 📄 Detalhes do pet (`/pets/{id}`)
- 🌐 Blog/artigos (se implementado)

**CSR (Client-Side Rendering)** para:
- 🔐 Dashboard do usuário (após login)
- 💬 Sistema de chat (SignalR)
- ❤️ Gerenciamento de favoritos
- 📝 Formulários de criação/edição de pets
- 📊 Painel administrativo

**Benefícios do Híbrido:**
- ✅ SEO otimizado (Google indexa conteúdo dos pets)
- ✅ Compartilhamento social com preview (Open Graph)
- ✅ Performance (páginas públicas carregam instantaneamente)
- ✅ Interatividade (dashboard tem atualizações em tempo real)
- ✅ Melhor experiência mobile (menos JavaScript inicial)

#### 🔗 Estrutura de URLs Híbrida

**Rotas Principais (Path-based):**
```
/pets                          → Lista todos os pets (SSR)
/pets/cachorro                 → Filtra por espécie (SSR)
/pets/gato                     → Filtra por espécie (SSR)
/pets/[id]                     → Detalhes do pet (SSR)
/pets/[species]/[city]         → Combina espécie + localização (SSR)
```

**Filtros Secundários (Query String):**
```
/pets/cachorro?breed=labrador&age=young&size=large
/pets/sao-paulo?species=gato&coat=curto&color=branco
/pets?state=sp&city=campinas&posted=last-week
```

**Vantagens da Abordagem Híbrida:**
- 🔍 **SEO:** URLs amigáveis para espécie e localização (principais filtros)
- 🔗 **Compartilhamento:** Links curtos e descritivos (`/pets/cachorro/sao-paulo`)
- 🎯 **Flexibilidade:** Filtros avançados via query string (sem poluir URL)
- 📊 **Analytics:** Fácil rastreamento das principais categorias
- 🚀 **Performance:** Next.js pré-renderiza rotas principais

**Exemplo de Implementação Next.js:**
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
  // SSR: busca na API durante o build/request
  const pets = await fetch(
    `${API_URL}/api/pets/search?species=${params.species}&city=${params.city}&age=${searchParams.age || ''}`
  )
  
  return <PetList pets={pets} />
}
```

#### 📱 Stack Tecnológica Recomendada

- **Framework:** Next.js 14+ (App Router)
- **Estilo:** Tailwind CSS + shadcn/ui
- **State:** Zustand (state client-side) + React Query (cache API)
- **Realtime:** SignalR Client (@microsoft/signalr)
- **Forms:** React Hook Form + Zod
- **Auth:** NextAuth.js v5 (integração JWT)

## 🧪 Testes

O projeto possui cobertura de **87.8%** com **203 testes** passando:

### Executar Testes
```bash
# Todos os testes
dotnet test

# Apenas testes unitários
dotnet test --filter "FullyQualifiedName~PetHub.Tests.UnitTests"

# Apenas testes de integração
dotnet test --filter "FullyQualifiedName~PetHub.Tests.IntegrationTests"

# Testes específicos de Refresh Token
dotnet test --filter "FullyQualifiedName~RefreshTokenTests"

# Com cobertura de código
dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"

# Gerar relatório HTML
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/coveragereport" -reporttypes:Html
```

### Estrutura de Testes
- **Integration Tests (178):** Testes de API end-to-end
  - **AuthController (11 testes):** Autenticação JWT + Refresh Token completo
    - Login com cookie HttpOnly
    - Refresh token com rotação automática
    - Detecção de reutilização de token (revoga todas as sessões)
    - Revogação explícita de token
    - Validação de tokens inválidos/expirados
  - **PetsController:** Busca, filtros, criação, edição, deleção
  - **UsersController:** CRUD, perfil, favoritos
- **Unit Tests (25):** Testes de lógica isolada (PasswordHelper, RefreshTokenHelper, etc.)

### Boas Práticas de Teste
- Uso de `TestConstants` para centralização de dados de teste
- `WebApplicationFactory` para testes de integração com banco in-memory
- FluentAssertions para asserções expressivas
- Isolamento total entre testes (cada teste usa instância isolada do banco)

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feat/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'feat: adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feat/nova-funcionalidade`)
5. Abra um Pull Request

### Padrão de Commits

Seguimos [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` Nova funcionalidade
- `fix:` Correção de bug
- `docs:` Documentação
- `test:` Adição/modificação de testes
- `refactor:` Refatoração de código
- `chore:` Tarefas de manutenção

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

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