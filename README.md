🐾 PetHub - Backend API

O PetHub é uma plataforma que conecta pessoas que desejam adotar animais de estimação com donos ou abrigos que possuem animais para adoção. Este repositório contém o Backend (API) da aplicação, construído com tecnologias modernas do ecossistema .NET.

🚀 Tecnologias Utilizadas

Linguagem: C# (.NET 8)

Framework: ASP.NET Core Web API

Banco de Dados: MySQL (Hospedado no TiDB Cloud Serverless)

ORM: Entity Framework Core 8 (Pomelo Provider)

Tempo Real: SignalR (Para o sistema de Chat)

Segurança: BCrypt (Hash de senhas)

Documentação: Swagger / OpenAPI

Ambiente de Dev: Docker & WSL 2

✨ Funcionalidades (Atuais)

👤 Gestão de Utilizadores (Users)

Registo Seguro: As senhas nunca são salvas em texto puro; utilizamos hash forte (BCrypt).

Endereço Completo: Estrutura preparada para receber dados de localização (CEP, Rua, Bairro, Cidade, Estado) para futuros filtros de proximidade.

Validação de Dados: O backend rejeita dados inválidos (ex: e-mail duplicado, formatos incorretos) usando DTOs e Regex.

🐶 Gestão de Pets (Em progresso)

Modelagem robusta para armazenar:

Idade em meses (para melhor ordenação).

Características físicas (Raça, Cor, Porte).

Múltiplas imagens por pet.

Filtros de adoção (Espécie, Género, Castrado/Vacinado).

💬 Comunicação & Adoção

Chat em Tempo Real: Arquitetura pronta com SignalR para conversas instantâneas entre adotante e dono.

Pedidos de Adoção: Fluxo formal para solicitar, aprovar ou rejeitar uma adoção.

Favoritos: Sistema para guardar pets de interesse.

🛠️ Configuração do Ambiente

Pré-requisitos

.NET 8 SDK instalado.

Acesso a um banco de dados MySQL (Recomendado: TiDB Cloud Serverless).

Git.

1. Clonar o Repositório

git clone [https://github.com/SEU-USUARIO/pethub.git](https://github.com/SEU-USUARIO/pethub.git)
cd pethub


2. Configurar Variáveis de Ambiente

Crie um ficheiro chamado .env na raiz do projeto (onde está o Program.cs).
Nota: Este ficheiro é ignorado pelo Git por segurança.

Adicione o seguinte conteúdo ao .env:

# Conexão com o Banco de Dados (TiDB / MySQL)
# Substitua USER, PASSWORD, HOST e PORT pelos seus dados reais.
DB_CONNECTION_STRING="Server=gateway01.us-east-1.prod.aws.tidbcloud.com;Port=4000;Database=test;Uid=SEU_USUARIO;Pwd=SUA_SENHA;SslMode=VerifyCA;"

# URLs permitidas para conectar no Chat/API (CORS)
# Separe por ponto e vírgula. Adicione a URL do Front (Vercel) quando tiver.
FRONTEND_URL="http://localhost:3000;http://localhost:5173"

# Chave de Segurança para futuros Tokens JWT (Digite uma frase longa aleatória)
JWT_SECRET="minha_chave_secreta_super_segura_pethub_2025"


3. Instalar Dependências

Restaure os pacotes do projeto:

dotnet restore


4. Configurar o Banco de Dados

Execute as migrações para criar as tabelas no seu banco MySQL remoto:

# Instale a ferramenta se ainda não tiver:
# dotnet tool install --global dotnet-ef

dotnet ef database update


Se ver a mensagem "Done.", as tabelas foram criadas com sucesso.

▶️ Como Rodar

Para iniciar o servidor de desenvolvimento:

dotnet run


Ou, se estiver a usar o VS Code, pressione F5.

A API estará disponível em:

Swagger (Documentação): http://localhost:5144/swagger (A porta pode variar, verifique o terminal).

API Base: http://localhost:5144/api

📂 Estrutura do Projeto

Controllers/: Pontos de entrada da API (Rotas HTTP).

Models/: Representação das tabelas do Banco de Dados.

DTOs/: (Data Transfer Objects) Objetos para entrada e saída de dados da API (Segurança e Validação).

Data/: Contexto do Banco de Dados (Entity Framework).

Hubs/: Lógica do Chat em Tempo Real (SignalR).

Services/: Lógica de negócios e integrações externas (ex: ViaCEP, Email).

Utils/: Funções auxiliares (ex: Hash de Senha).

Middlewares/: Tratamento global de erros.

🚢 Deploy (Produção)

Este projeto está configurado para ser hospedado no Render (via Docker).

O Dockerfile na raiz cria a imagem otimizada.

O Program.cs lê as variáveis de ambiente (DB_CONNECTION_STRING) injetadas pelo painel do Render.

O Frontend (React) deve ser hospedado na Vercel.


Desenvolvido com 💜 por Christian Volz