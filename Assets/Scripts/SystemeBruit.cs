using System;
using UnityEngine;

// SYSTEME DE BRUIT : point central du son dans le jeu.
// N'importe quoi peut faire du bruit : SystemeBruit.Emettre(position, portee).
// (pas du joueur, objet ramasse, porte, objet lance...). L'ennemi aveugle ecoute.
// portee = distance en metres a laquelle un ennemi normal entend le bruit.
public static class SystemeBruit
{
    public static event Action<Vector3, float> BruitEmis;

    public static void Emettre(Vector3 position, float portee)
    {
        BruitEmis?.Invoke(position, portee);
    }
}