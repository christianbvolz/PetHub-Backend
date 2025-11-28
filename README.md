# 🐾 PetHub - Backend API

O PetHub é uma plataforma que conecta pessoas que desejam adotar animais de estimação com donos ou abrigos que possuem animais para adoção. Este repositório contém o Backend (API) da aplicação, construído com tecnologias modernas do ecossistema .NET.

[![CI](https://github.com/christianbvolz/PetHub-Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/christianbvolz/PetHub-Backend/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-43%20passing-brightgreen)](tests/)
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
- **Testes:** xUnit + FluentAssertions (43 testes de integração)
- **Padrões:** Repository Pattern, DTOs, Dependency Injection
- **CI/CD:** GitHub Actions

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

### 📊 Sistema de Tags
- **Categorias:** Color (Cor), Pattern (Padrão), Coat (Pelagem)
- Permite classificação flexível dos pets
- Suporte a múltiplas tags por pet
- Filtros AND/OR configuráveis

### 👤 Gestão de Utilizadores (Users)
- Registro seguro com hash BCrypt
- Endereço completo para filtros de proximidade
- Validação de dados (email duplicado, formatos)
- DTOs para segurança e validação

### 💬 Comunicação & Adoção (Estrutura Base)
- **Chat em Tempo Real:** SignalR configurado
- **Pedidos de Adoção:** Modelo de dados pronto
- **Favoritos:** Estrutura preparada

## 🧪 Testes

O projeto possui uma suite completa de **43 testes de integração** com 100% de aprovação:

- **GetPet:** 11 testes (validação de ID, relacionamentos, erros)
- **SearchPets:** 14 testes (filtros, paginação, ordenação)
- **CreatePet:** 18 testes (validações, relacionamentos, autenticação)

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
│       ├── Controllers/          # Endpoints HTTP (PetsController, UsersController)
│       ├── Models/               # Entidades do banco (Pet, User, Species, etc)
│       ├── DTOs/                 # Data Transfer Objects
│       │   ├── Pet/              # CreatePetDto, PetResponseDto, SearchPetsQuery
│       │   ├── User/             # UserResponseDto, CreateUserDto
│       │   └── Common/           # PagedResult<T>
│       ├── Services/             # Lógica de negócio
│       │   ├── IPetRepository.cs # Interface do repositório
│       │   └── PetRepository.cs  # Implementação com EF Core
│       ├── Mappings/             # Extension methods para mapear entidades → DTOs
│       ├── Data/                 # Contexto EF Core + Migrations + Seeding
│       ├── Enums/                # PetGender, PetSize, TagCategory, etc
│       ├── Hubs/                 # SignalR hubs (Chat em tempo real)
│       ├── Middlewares/          # GlobalExceptionMiddleware
│       ├── Utils/                # PasswordHelper (BCrypt)
│       └── Program.cs            # Entry point + configuração
│
├── tests/
│   └── PetHub.Tests/            # Testes de integração (xUnit)
│       └── IntegrationTests/
│           ├── GetPetIntegrationTests.cs        # 11 testes
│           ├── SearchPetsIntegrationTests.cs    # 14 testes
│           ├── CreatePetIntegrationTests.cs     # 18 testes
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
| `POST` | `/api/pets` | Criar novo pet | ✅ Implementado |
| `PUT` | `/api/pets/{id}` | Atualizar pet | 🚧 Planejado |
| `DELETE` | `/api/pets/{id}` | Remover pet | 🚧 Planejado |

### 👤 Usuários

| Método | Endpoint | Descrição | Status |
|--------|----------|-----------|--------|
| `POST` | `/api/users/register` | Registrar novo usuário | 🚧 Planejado |
| `POST` | `/api/users/login` | Login JWT | 🚧 Planejado |
| `GET` | `/api/users/{id}` | Perfil do usuário | 🚧 Planejado |
| `PUT` | `/api/users/{id}` | Atualizar perfil | 🚧 Planejado |

### 💬 Chat & Adoção

| Método | Endpoint | Descrição | Status |
|--------|----------|-----------|--------|
| `POST` | `/api/adoption-requests` | Solicitar adoção | 🚧 Planejado |
| `SignalR` | `/hubs/chat` | Chat em tempo real | 🚧 Implementado (base) |

## 🎯 Próximos Passos

- [ ] Implementar autenticação JWT
- [ ] Adicionar endpoints de CRUD completo para Users
- [ ] Implementar sistema de favoritos
- [ ] Completar fluxo de pedidos de adoção
- [ ] Adicionar upload de imagens real (S3/Cloudinary)
- [ ] Implementar filtros geográficos (proximidade)
- [ ] Adicionar rate limiting
- [ ] Implementar cache (Redis)
- [ ] Adicionar logging estruturado (Serilog)
- [ ] Implementar health checks
- [ ] Adicionar testes unitários (além dos de integração)

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