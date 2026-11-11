using Godot;

/// <summary>
/// Escena raíz del prototipo. Por ahora solo pinta el fondo y loguea el arranque.
/// Acá va a vivir el futuro GameManager / cambio de escenas (mapa, batalla FF5, etc).
/// </summary>
public partial class Main : Node2D
{
	public override void _Ready()
	{
		GD.Print("Code Isekai listo: un dev llega al isekai. A vencer al Rey Demonio.");
	}

	public override void _Process(double delta)
	{
		// Loop futuro: estados de juego, input del party, triggers de retos.
	}
}
