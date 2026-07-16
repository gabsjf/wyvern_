<div align="center">

# 🐉 Wyvern

**Plataforma de gerenciamento de campanhas de RPG de mesa**
</div>

---

##  Sobre o Projeto

Campanhas de RPG costumam acumular uma grande quantidade de informações distribuídas entre anotações, planilhas, documentos e mensagens. O **Wyvern** resolve esse problema fornecendo uma plataforma estruturada para centralizar e gerenciar todos os dados relevantes de uma campanha em um único ambiente.

A aplicação permite que mestres e jogadores tenham acesso rápido às informações necessárias, reduzindo a complexidade administrativa e aumentando o foco na experiência de jogo.

---

##  Funcionalidades

<table>
<tr>
<td width="50%">

###  Gestão de Campanhas
- Cadastro de campanhas
- Organização de sessões
- Controle cronológico de acontecimentos
- Registro de eventos importantes

###  Gestão de Personagens
- Criação de personagens
- Armazenamento de atributos
- Controle de evolução
- Histórico do personagem

</td>
<td width="50%">

###  Gestão de Jogadores
- Associação entre jogadores e personagens
- Controle de participação em campanhas
- Administração de grupos

###  Sessões
- Numeração automática de sessões
- Registro de resumos
- Histórico completo da campanha
- Consulta de sessões anteriores

</td>
</tr>
</table>

---

##  Arquitetura

O projeto segue princípios de **Clean Architecture** com separação clara de responsabilidades em camadas.

```
Wyvern
│
├── Wyvern.Api              # Endpoints HTTP, Controllers, Validações
├── Wyvern.Application      # Casos de uso, Services, Regras de aplicação
├── Wyvern.Domain           # Entidades, Regras de negócio, Contratos
└── Wyvern.Infrastructure   # EF Core, Repositórios, Persistência
```

### Fluxo da Aplicação

```
Controller  →  Application Service  →  Repository  →  DbContext  →  Database
```

> Este fluxo garante que as regras de negócio permaneçam isoladas da tecnologia utilizada para persistência dos dados.

### Detalhamento das Camadas

| Camada | Responsabilidade |
|---|---|
| **Wyvern.Api** | Controllers, endpoints REST, validação de requisições, serialização de respostas |
| **Wyvern.Application** | Services, regras de aplicação, orquestração de operações, comunicação entre API e domínio |
| **Wyvern.Domain** | Entidades, regras de negócio, contratos, objetos de valor, modelagem do domínio |
| **Wyvern.Infrastructure** | Entity Framework Core, repositórios, persistência de dados, integrações externas |

---

## 🛠️ Tecnologias

**Backend**
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) — Framework web
- [C#](https://docs.microsoft.com/dotnet/csharp) — Linguagem principal
- [Entity Framework Core](https://docs.microsoft.com/ef/core) — ORM
- [SQL Server](https://www.microsoft.com/sql-server) — Banco de dados

**Frontend**
- [Angular](https://angular.io) — Framework frontend
- [TypeScript](https://www.typescriptlang.org) — Linguagem
- [SCSS](https://sass-lang.com) — Estilização

**Ferramentas**
- Visual Studio / Visual Studio Code
- Git & GitHub

---

##  Como Executar

### Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) e [Angular CLI](https://angular.io/cli)
- [SQL Server](https://www.microsoft.com/sql-server)

### Backend

```bash
# Clone o repositório
git clone <repository-url>

# Acesse a pasta da API
cd Wyvern.Api

# Restaure os pacotes
dotnet restore

# Execute as migrations
dotnet ef database update

# Inicie a aplicação
dotnet run
```

### Frontend

```bash
# Acesse a pasta do frontend
cd Wyvern.Front/Wyvern

# Instale as dependências
npm install

# Inicie o servidor de desenvolvimento
ng serve
```

A aplicação estará disponível em **http://localhost:4200**

---

## 🗺️ Roadmap

### MVP
- [x] Cadastro de campanhas
- [x] Cadastro de personagens
- [x] Cadastro de jogadores
- [x] Controle de sessões
- [x] Persistência em banco de dados

### Próximas Versões
- [ ] Sistema de permissões
- [ ] Compartilhamento de campanhas
- [ ] Upload de arquivos
- [ ] Diário da campanha
- [ ] Integração com fichas digitais
- [ ] Dashboard de estatísticas
- [ ] Aplicação mobile

---

##  Princípios de Desenvolvimento

| Princípio | Descrição |
|---|---|
| **Clean Architecture** | Separação entre domínio e infraestrutura para reduzir acoplamento |
| **SOLID** | Aplicação dos princípios SOLID para aumentar legibilidade e extensibilidade |
| **Repository Pattern** | Abstração da camada de acesso a dados |
| **Dependency Injection** | Gerenciamento de dependências via container nativo do ASP.NET Core |

---

##  Motivação

O Wyvern nasceu como um projeto de estudo e evolução técnica, servindo como ambiente para aprofundamento em:

- ASP.NET Core & Entity Framework Core
- Arquitetura em Camadas & Clean Architecture
- Angular & Desenvolvimento Full Stack
- Modelagem de Domínio

Além do aprendizado técnico, o projeto busca solucionar problemas reais enfrentados por grupos de RPG durante a organização e acompanhamento de campanhas de longa duração.

---

##  Licença

Este projeto está em desenvolvimento e seu código é disponibilizado para fins **educacionais e de aprendizado**.

---

<div align="center">
  <sub>Feito com ❤️ para a comunidade de RPG</sub>
</div>
