namespace IdentityService.IntegrationTests.Infrastructure;

/// <summary>Помечает тест, который обязательно выполняется с изолированной БД в Docker.</summary>
public sealed class DockerFactAttribute : FactAttribute;

/// <summary>Помечает параметризованный тест с обязательной изолированной БД в Docker.</summary>
public sealed class DockerTheoryAttribute : TheoryAttribute;
