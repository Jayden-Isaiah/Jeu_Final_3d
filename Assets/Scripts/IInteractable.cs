// Contrat commun a tout objet avec lequel le joueur peut interagir (collectable, porte, levier...).
public interface IInteractable
{
    // Texte affiche quand le joueur regarde l'objet (ex. "Ramasser le fusible").
    string TexteInteraction { get; }

    // Appele quand le joueur appuie sur la touche d'interaction en regardant l'objet.
    void Interagir(InteractionJoueurFPS joueur);
}