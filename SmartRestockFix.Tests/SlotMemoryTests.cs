using System;
using System.IO;
using SmartRestockFix;
using Xunit;

public class SlotMemoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "slotmemory-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void LembraESobreviveAReabrirOJogo()
    {
        var memory = new SlotMemory(_dir);
        memory.UseSaveSlot(1);
        memory.Remember("Barn:LowerGrid:1,0:largestoragerack:6", "ogkushseed");
        memory.Flush();

        var reaberto = new SlotMemory(_dir);
        reaberto.UseSaveSlot(1);
        Assert.Equal("ogkushseed", reaberto.Recall("Barn:LowerGrid:1,0:largestoragerack:6"));
    }

    [Fact]
    public void CadaSaveTemSuaPropriaMemoria()
    {
        var memory = new SlotMemory(_dir);
        memory.UseSaveSlot(1);
        memory.Remember("slot", "ogkushseed");
        memory.UseSaveSlot(2); // troca de save grava o anterior

        Assert.Null(memory.Recall("slot"));
        memory.UseSaveSlot(1);
        Assert.Equal("ogkushseed", memory.Recall("slot"));
    }

    [Fact]
    public void TrocaDeProdutoAtualizaEIgnoraRepetido()
    {
        var memory = new SlotMemory(_dir);
        memory.UseSaveSlot(1);

        Assert.True(memory.Remember("slot", "ogkushseed"));
        Assert.False(memory.Remember("slot", "ogkushseed"));
        Assert.True(memory.Remember("slot", "sourdieselseed"));
        Assert.Equal("sourdieselseed", memory.Recall("slot"));
    }

    [Fact]
    public void IgnoraValoresVazios()
    {
        var memory = new SlotMemory(_dir);
        memory.UseSaveSlot(1);

        Assert.False(memory.Remember("slot", null));
        Assert.False(memory.Remember(null, "ogkushseed"));
        Assert.Equal(0, memory.Count);
    }

    [Fact]
    public void SemMudancaNaoCriaArquivo()
    {
        var memory = new SlotMemory(_dir);
        memory.UseSaveSlot(3);
        memory.Flush();

        Assert.False(File.Exists(memory.FilePath(3)));
    }
}
