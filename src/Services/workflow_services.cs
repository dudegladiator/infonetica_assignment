// src/Services/WorkflowService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Infonetica.src.Models;

namespace Infonetica.src.Services
{
    public class WorkflowService
    {
        private const string DefinitionsFile = "workflows.json";
        private const string InstancesFile   = "instances.json";

        private readonly Dictionary<string, WorkflowDefinition> _definitions;
        private readonly Dictionary<string, WorkflowInstance>   _instances;

        public WorkflowService()
        {
            // --- Load definitions ---
            if (File.Exists(DefinitionsFile))
            {
                var defsJson = File.ReadAllText(DefinitionsFile);
                var defsList = JsonSerializer.Deserialize<List<WorkflowDefinition>>(defsJson)
                              ?? new List<WorkflowDefinition>();
                _definitions = defsList.ToDictionary(d => d.Id);
            }
            else
            {
                _definitions = new();
            }

            // --- Load instances ---
            if (File.Exists(InstancesFile))
            {
                var instJson = File.ReadAllText(InstancesFile);
                var instList = JsonSerializer.Deserialize<List<WorkflowInstance>>(instJson)
                              ?? new List<WorkflowInstance>();
                _instances = instList.ToDictionary(i => i.Id);
            }
            else
            {
                _instances = new();
            }
        }

        #region Persistence

        private void PersistDefinitions()
        {
            var json = JsonSerializer.Serialize(
                _definitions.Values,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(DefinitionsFile, json);
        }

        private void PersistInstances()
        {
            var json = JsonSerializer.Serialize(
                _instances.Values,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(InstancesFile, json);
        }

        #endregion

        #region Definition APIs

        public (bool Success, string? Error) CreateDefinition(WorkflowDefinition def)
        {
            // 1) Unique Id?
            if (_definitions.ContainsKey(def.Id))
                return (false, $"A workflow with Id '{def.Id}' already exists.");

            // 2) Exactly one initial state?
            var initialCount = def.States.Count(s => s.IsInitial);
            if (initialCount != 1)
                return (false, $"Must mark exactly one state as initial (found {initialCount}).");

            // 3) No duplicate state IDs
            var dupStates = def.States
                               .GroupBy(s => s.Id)
                               .Where(g => g.Count() > 1)
                               .Select(g => g.Key)
                               .ToList();
            if (dupStates.Any())
                return (false, $"Duplicate state IDs: {string.Join(", ", dupStates)}.");

            // 4) Actions reference only valid states
            var validStateIds = def.States.Select(s => s.Id).ToHashSet();
            foreach (var action in def.Actions)
            {
                if (!validStateIds.Contains(action.ToState))
                    return (false, $"Action '{action.Id}' has invalid toState '{action.ToState}'.");

                var badFrom = action.FromStates
                                     .Where(fs => !validStateIds.Contains(fs))
                                     .ToList();
                if (badFrom.Any())
                    return (false, $"Action '{action.Id}' has invalid fromStates: {string.Join(", ", badFrom)}.");
            }

            // All good—store and persist
            _definitions[def.Id] = def;
            PersistDefinitions();
            return (true, null);
        }

        public WorkflowDefinition? GetDefinition(string id)
            => _definitions.TryGetValue(id, out var def) ? def : null;

        public IEnumerable<WorkflowDefinition> GetAllDefinitions()
            => _definitions.Values;

        #endregion

        #region Runtime APIs

        public (bool Success, string? Error, WorkflowInstance? Instance) StartInstance(string defId, string description)
        {
            if (!_definitions.TryGetValue(defId, out var def))
                return (false, $"Definition '{defId}' not found.", null);

            var initial = def.States.SingleOrDefault(s => s.IsInitial);
            if (initial is null)
                return (false, "Definition has no initial state.", null);

            var inst = new WorkflowInstance
            {
                DefinitionId = defId,
                CurrentState  = initial.Id,
                Description   = description
            };

            _instances[inst.Id] = inst;
            PersistInstances();
            return (true, null, inst);
        }

        public (bool Success, string? Error, WorkflowInstance? Instance) ExecuteAction(string instId, string actionId)
        {
            if (!_instances.TryGetValue(instId, out var inst))
                return (false, $"Instance '{instId}' not found.", null);

            var def = _definitions[inst.DefinitionId];

            // Cannot act on a final state
            var currState = def.States.Single(s => s.Id == inst.CurrentState);
            if (currState.IsFinal)
                return (false, $"Instance is already in final state '{currState.Id}'.", null);

            // Verify action exists
            var action = def.Actions.SingleOrDefault(a => a.Id == actionId);
            if (action is null)
                return (false, $"Action '{actionId}' not found in definition.", null);

            if (!action.Enabled)
                return (false, $"Action '{actionId}' is disabled.", null);

            if (!action.FromStates.Contains(inst.CurrentState))
                return (false, $"Action '{actionId}' cannot be applied from state '{inst.CurrentState}'.", null);

            // All checks passed—transition
            inst.CurrentState = action.ToState;
            inst.History.Add(new HistoryEntry(actionId, DateTime.UtcNow));

            PersistInstances();
            return (true, null, inst);
        }

        public WorkflowInstance? GetInstance(string instId)
            => _instances.TryGetValue(instId, out var inst) ? inst : null;

        public IEnumerable<WorkflowInstance> GetAllInstances()
            => _instances.Values;

        #endregion
    }
}
