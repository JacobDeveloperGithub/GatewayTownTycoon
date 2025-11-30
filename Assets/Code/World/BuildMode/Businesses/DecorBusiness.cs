using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DecorBusiness : Business, IPopularityEffect {
    protected SpriteRenderer _renderer;

    public int GetPopularityChange() => WorldContext.ReputationImpact;
}