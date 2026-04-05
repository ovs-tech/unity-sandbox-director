using System;
using System.Linq;
using Unity.AppUI.Redux;
using UnityEngine;

namespace Systems.UI
{
    [Serializable]
    public record ListProjectState
    {
        [SerializeField]
        public Project[] projects = Array.Empty<Project>();

        [SerializeField]
        public string searchInput;

        [SerializeField]
        public string filter;

        public override string ToString()
        {
            var projectsString = projects == null ? "null" : projects.Length == 0 ? "[]" : projects.Select(project => project.ToString()).Aggregate((a, b) => $"        {a},\n        {b}");
            return @$"{{
            searchInput: {searchInput},
            filter: {filter},
            projects: {projectsString}
        }}";
        }
    }

    class ListProjectReducers
    {
        // Reducer for loading projects (placeholder, implement actual logic)
        public static ListProjectState CreateProjectReducer(ListProjectState state, IAction<string> action)
        {
            // Placeholder logic for creating a project
            var newProject = new Project { name = action.payload, meta = "New project" };
            var newProjects = new Project[state.projects.Length + 1];
            state.projects.CopyTo(newProjects, 0);
            newProjects[newProjects.Length - 1] = newProject;
            return state with { projects = newProjects };
        }

        public static ListProjectState EditProjectReducer(ListProjectState state, IAction<(string oldName, string newName)> action)
        {
            var (oldName, newName) = action.payload;
            var newProjects = state.projects.Select(project =>
                project.name == oldName ? project with { name = newName } : project
            ).ToArray();
            return state with { projects = newProjects };
        }

        public static ListProjectState DeleteProjectReducer(ListProjectState state, IAction<string> action)
        {
            var projectNameToDelete = action.payload;
            var newProjects = state.projects.Where(project => project.name != projectNameToDelete).ToArray();
            return state with { projects = newProjects };
        }

        public static ListProjectState SetSearchInputReducer(ListProjectState state, IAction<string> action)
        {
            return state with { searchInput = action.payload };
        }

        public static ListProjectState SetFilterReducer(ListProjectState state, IAction<string> action)
        {
            return state with { filter = action.payload };
        }
    }

    public static class ListProjectActions
    {
        internal static readonly ActionCreator<string> createProject = "app/CreateProject";
        internal static readonly ActionCreator<(string oldName, string newName)> editProject = "app/EditProject";
        internal static readonly ActionCreator<string> deleteProject = "app/DeleteProject";
        internal static readonly ActionCreator<string> setSearchInput = "app/SetSearchInput";
        internal static readonly ActionCreator<string> setFilter = "app/SetFilter";
    }
}