# PRD – Audit / Change Tracking API

## 1. Overview
Nom du produit : Audit / Change Tracking API  
Type : Librairie .NET (NuGet)  
Cible : ASP.NET Core / EF Core / SQL Server  

Objectif : Fournir un mécanisme centralisé pour tracer et historiser toutes les modifications des entités EF Core, permettant audit, conformité et analyse des changements.

---

## 2. Problème
- Modifications de données dispersées et non tracées
- Difficulté à retrouver qui a modifié quoi et quand
- Compliance (RGPD, ISO, SOX) souvent non respectée
- Code de suivi répétitif et fragile

---

## 3. Objectifs
- Historiser toutes les modifications CRUD des entités
- Capturer : qui, quand, quoi, valeur précédente, valeur nouvelle
- Fournir un accès simple aux logs pour audits ou reporting
- Support multi-tenant et filtrage dynamique
- Extensible sans modifier le code métier

---

## 4. Concepts clés
### Entité Audit
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; }
    public string EntityId { get; set; }
    public string Action { get; set; } // Create, Update, Delete
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
}
```

### Actions supportées
- Create
- Update
- Delete
- Soft Delete (optionnel)

### Optionnel
- Multi-tenant : `TenantId`
- Relation avec utilisateur pour `ChangedBy`
- Historisation des relations / collections

---

## 5. Stockage EF Core
- Table `AuditLogs` optimisée pour insert massif
- Index sur `EntityName`, `EntityId`, `ChangedAt`
- Chargement rapide pour reporting et audit
- Option de purge automatique ou archivage

---

## 6. API publique
### Service principal
```csharp
Task LogAsync<T>(T entity, string action, string userId);
Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName, string entityId);
```

### Middleware / DbContext Interceptor
- Intercepte automatiquement `SaveChangesAsync`  
- Capture les modifications et génère les logs  
- Compatible transactions EF Core  

### Exemple d'utilisation
```csharp
await auditService.LogAsync(product, "Update", currentUserId);
var logs = await auditService.GetAuditLogsAsync("Product", product.Id.ToString());
```

---

## 7. Sécurité
- Contrôle accès lecture des logs (rôle `Auditor`, `Admin`)  
- Logs immuables (insert-only)  
- Option chiffrement des données sensibles  
- Protection contre suppression accidentelle

---

## 8. Performance
- Interception légère et asynchrone  
- Inserts batch pour volume élevé  
- Indexation SQL Server pour requêtes rapides  
- Possibilité de désactiver pour certaines entités ou colonnes

---

## 9. Livrables
- Package NuGet `AuditTracking.API`  
- README avec configuration et exemples  
- Tests unitaires et d’intégration  
- Exemple ASP.NET Core complet avec DbContext interceptor

---

## 10. Évolutions futures
- Support EF Core temporal tables  
- Notifications en cas de modification critique  
- Export CSV / JSON pour audit externe  
- Dashboard de suivi des changements  
- Historisation relationnelle (collections et liens entre entités)
