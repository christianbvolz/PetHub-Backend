# Testes de Integração - PetHub API

## 📋 Sobre

Este diretório contém os testes de integração para a API PetHub. Os testes validam o comportamento completo dos endpoints, incluindo:

- Requisições HTTP
- Interações com banco de dados
- Serialização/Deserialização JSON
- Validações de negócio
- Estrutura de resposta

## 🏗️ Estrutura

```
tests/PetHub.Tests/
├── IntegrationTests/
│   ├── PetHubWebApplicationFactory.cs  # Factory para criar servidor de testes
│   ├── TestDataSeeder.cs                # Popula dados de teste no banco
│   └── SearchPetsIntegrationTests.cs    # Testes do endpoint SearchPets
├── GlobalUsings.cs
└── PetHub.Tests.csproj
```

## 🧪 Testes Implementados

### SearchPetsIntegrationTests

Valida o funcionamento do endpoint `GET /api/pets/search`:

#### ✅ Cenários Testados:

1. **SearchPets_WithoutFilters_ReturnsAllAvailablePets**
   - Busca sem filtros retorna todos os pets disponíveis
   - Valida paginação básica

2. **SearchPets_WithPagination_ReturnsCorrectPage**
   - Paginação funciona corretamente
   - Valida `HasNextPage` e `HasPreviousPage`

3. **SearchPets_FilterBySpecies_ReturnsOnlyMatchingPets**
   - Filtro por espécie (Cachorro/Gato)
   - Retorna apenas pets da espécie solicitada

4. **SearchPets_FilterByGender_ReturnsOnlyMatchingPets**
   - Filtro por gênero (Male/Female)
   - Valida enum de gênero

5. **SearchPets_FilterBySize_ReturnsOnlyMatchingPets**
   - Filtro por tamanho (Small/Medium/Large)
   - Valida enum de tamanho

6. **SearchPets_FilterByBreed_ReturnsOnlyMatchingPets**
   - Filtro por raça (Labrador, Poodle, etc.)
   - Busca parcial (LIKE)

7. **SearchPets_FilterByColor_ReturnsOnlyMatchingPets**
   - Filtro por cor única
   - Valida tags de cor

8. **SearchPets_FilterByMultipleColors_ReturnsMatchingPets**
   - Filtro por múltiplas cores separadas por vírgula
   - Busca OR entre cores

9. **SearchPets_FilterByCoat_ReturnsOnlyMatchingPets**
   - Filtro por tipo de pelagem
   - Valida tags de coat

10. **SearchPets_CombinedFilters_ReturnsCorrectResults**
    - Combinação de múltiplos filtros
    - Validação lógica AND

11. **SearchPets_NoMatchingResults_ReturnsEmptyList**
    - Busca sem resultados retorna lista vazia
    - Status 200 OK mesmo sem resultados

12. **SearchPets_InvalidPageNumber_ReturnsEmptyList**
    - Página inexistente retorna vazio
    - Não gera erro

13. **SearchPets_ResponseStructure_IsCorrect**
    - Valida estrutura completa do DTO
    - Todos os campos obrigatórios presentes

14. **SearchPets_ExcludesAdoptedPets_ByDefault**
    - Pets adotados não aparecem na busca
    - Apenas pets disponíveis são retornados

## 🚀 Como Executar

### Executar todos os testes:
```bash
dotnet test
```

### Executar apenas testes de integração:
```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Executar teste específico:
```bash
dotnet test --filter "FullyQualifiedName~SearchPets_WithoutFilters"
```

### Com verbosidade detalhada:
```bash
dotnet test --verbosity detailed
```

### Com relatório de cobertura:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Cobertura de Testes

Os testes cobrem:
- ✅ Controllers
- ✅ Repositories
- ✅ DTOs
- ✅ Validações
- ✅ Mapeamentos
- ✅ Filtros de busca
- ✅ Paginação

## 🔧 Tecnologias Utilizadas

- **xUnit**: Framework de testes
- **FluentAssertions**: Asserções expressivas
- **Microsoft.AspNetCore.Mvc.Testing**: Testes de integração ASP.NET Core
- **EntityFrameworkCore.InMemory**: Banco de dados em memória para testes

## 💡 Boas Práticas Implementadas

1. **Isolamento**: Cada teste usa seu próprio banco de dados
2. **AAA Pattern**: Arrange, Act, Assert
3. **Nomes Descritivos**: Nome do teste descreve o cenário
4. **Dados de Teste**: Seeder reutilizável
5. **Cleanup**: Dispose correto dos recursos
6. **Factory Pattern**: WebApplicationFactory para servidor de testes
7. **Assertions Fluentes**: FluentAssertions para legibilidade

## 📝 Adicionar Novos Testes

### Exemplo de novo teste:

```csharp
[Fact]
public async Task SearchPets_NewScenario_ExpectedBehavior()
{
    // Arrange
    var requestUri = "/api/pets/search?param=value";

    // Act
    var response = await _client.GetAsync(requestUri);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<PagedResult<PetResponseDto>>();
    result.Should().NotBeNull();
    // ... more assertions
}
```

## 🐛 Troubleshooting

### Testes falhando com "Connection refused":
- Verifique se não há outra instância da API rodando
- O `WebApplicationFactory` cria seu próprio servidor de testes

### Dados inconsistentes:
- Cada teste tem seu próprio banco isolado
- O seeder é executado antes de cada teste

### Timeout nos testes:
- Verifique configurações do banco in-memory
- Aumente o timeout se necessário:
```csharp
[Fact(Timeout = 10000)] // 10 segundos
```

## 📚 Referências

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [EF Core In-Memory Provider](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)
