from openai import OpenAI
import os
import json
from typing import List
import matplotlib.pyplot as plt



import networkx as nx

from pydantic import BaseModel


# Goal set
GOALS = ["Save a cat from a tall tree",
         "Make a makeshift boat to transport a small object across a pond",
         "Threaten someone to do your bidding"]

# Object sets
LIST_OF_OBJECTS_1 = ["bucket", "hammer", "coffee cup", "alcohol spray", "umbrella"]
LIST_OF_OBJECTS_2 = ["watch", "hose", "water bottle", "cooking pan", "duct tape"]


# Given a set of interactions and edges, build a directed graph
def build_puzzle_graph(interactions_json: str, edges_json: str) -> nx.DiGraph:
        interactions_data = json.loads(interactions_json)
        edge_data = json.loads(edges_json)
        interactions = interactions_data.get("interactions", [])
        edges = edge_data.get("edges", [])
        G = nx.DiGraph()
        for it in interactions:
            node_id = it.get("id")
            G.add_node(
                node_id,
                description=it.get("description", ""),
                required_objects=tuple(it.get("required_objects", [])),
                effect=it.get("effect", "")
            )
        for u, v in edges:
            if u in G and v in G:
                G.add_edge(u, v)
        return G


# Draw the directed graph and save to file
def draw_graph(G: nx.DiGraph, title: str, out_path: str):
    pos = nx.spring_layout(G, seed=42)
    plt.figure(figsize=(9, 6))
    nx.draw(G, pos, with_labels=True, node_size=1600, font_size=9)
    labels = {n: (G.nodes[n].get("description","").split(".")[0][:40]) for n in G.nodes}
    nx.draw_networkx_labels(G, pos, labels=labels, font_size=7, verticalalignment="bottom")
    plt.title(title)
    plt.savefig(out_path, dpi=160)
    plt.close()


class RealObject(BaseModel):
    name: str
    description: str

# Object Analyzer Structured Output
class OjectAnalyzerOutput(BaseModel):
    objects: List[RealObject]

# Affordance Generator Structured Output
class Affordance(BaseModel):
    object_name: str
    affordance: str
    rationale: str

# Affordance List Output
class AffordanceListOutput(BaseModel):
    affordances: List[Affordance]

# Interaction Generator Structured Output
class Interaction(BaseModel):
    id: str
    description: str
    required_objects: List[str]
    effect: str

# Interaction List Output
class InteractionListOutput(BaseModel):
    interactions: List[Interaction]

# Edge List Structured Output
class EdgeList(BaseModel):
    edges: List[List[str]]

current_goals = GOALS[0]


# Main pipeline function
def pipeline(type_of_run):

    client = initialize_openai()

    # change current objects given current run type
    if type_of_run == 1:
        current_objects = LIST_OF_OBJECTS_1
    else:
        current_objects = LIST_OF_OBJECTS_2

    
    # iterate through all goals
    for i in range(len(GOALS)):
        current_goals = GOALS[i]

        print("Current Goal: ", current_goals)
        print("Current Objects: ", current_objects)

        # Step 1: Object Analyzer
        OBJECT_ANALYZER_PROMPT = (
            f"Goal: {current_goals}\n"
            f'Task: Generate detailed yet concise descriptions for each object that could help achieve the goal.\n'
            f"Objects: {', '.join(current_objects)}\n\n"
            "Output JSON format:\n"
            '{ "objects": [ { "name": str, "description": str }, ... ] }\n'
            "Reminders:\n"
            "Name of objects should be exactly the same as listed in Objects section as prompt. Also keep discriptions to one sentence, but have 2-3 descriptions per object\n"
        )

        # Step 2: Affordance Generator
        AFFORDANCE_GENERATOR_PROMPT = (
            f"Goal: {current_goals}\n"
            "Task: From the object analyses below, list atleast 3 affordances per object. These affordances should be relevant to the goal\n"
            "Use ONLY the objects and info from this JSON:\n"
            "{OBJECT_ANALYZER_JSON}\n\n"
            "Output JSON schema:\n"
            '{ "affordances": [ { "object_name": str, "affordance": str, "rationale": str }] }\n'
            "Reminders:\n"
            "Name of objects should be exactly the same as input list of objects. Affordance is a capabiity of the object, and rationale is a short phrase linking the capability to the goal. Also, all objects MUST be given affordances.\n"
        )

        # Step 3: Affordance Filter
        AFFORDANCE_FILTER_PROMPT = (
            f"Goal: {current_goals}\n"
            "Task: Filter the affordance list to only those that are helpful to the goal. Remove any affordances that are irrelevant or not neccesary to the goal, but ensure that you have atleast one affordance per object.\n"
            "Use ONLY this JSON as input:\n"
            "{AFFORDANCE_LIST_JSON}\n\n"
            "Output JSON schema:\n"
            '{ "affordances": [ { "object_name": str, "affordance": str, "rationale": str }] }\n'
            "Reminders:\n"
            "Try to shorten it to 1-2 more important affordances per object that are absolutely neccesary to achieve the task. Remember that all objects MUST be used in the final interaction list to achieve the goal, so atleast one affordance should be present for each object. This is the most important thing, atleast one affordance should be present per object.\n\n"
        )

        # Step 4: Interaction Generator
        INTERACTION_GENERATOR_PROMPT = (
            f"Goal: {current_goals}\n"
            "Task: Using ONLY the filtered affordances below, generate however many interactions neccesary so that all the objects work together to form a coherent plan of achieving the goal\n"
            "Each interaction must reference one or more real objects from the input, include minimal preconditions, and build into the next interaction with a different object. "
            "describe the state-change effect, amd all objects must be used. In addition, the interactions should create a connected graph when transfered to edges and graphed. This ensures that all objects work together to achieve the goal.\n"
            "Use ONLY this JSON as input:\n"
            "{FILTERED_AFFORDANCES_JSON}\n\n"
            "Output JSON schema:\n"
            '{ "interactions": [\n'
            '  {\n'
            '    "id": str,\n'
            '    "description": str,\n'
            '    "required_objects": [str, ...],\n'
            '    "effect": str,\n'
            '] }\n'
            "Reminders:\n"
            "id: use numbers (1, 2, 3, ...) and a shortened description of the interaction as the id (1_Fill_Bucket_With_Water). This will be shown on a graph later, so it should be understandable.\n"
            "all objects in the set must be named exactly the same as the input and each object MUST be used atleast once in the steps to achieve the goal. Do not add any objects not stated in input affordance.\n"
            "effect: one-line state change (dough: rolled -> flatted)\n"
        )

        # Step 5: Graph Edge Generator
        EDGE_GENERATOR_PROMPT = (
            f"Goal: {current_goals}\n"
            "Task: Produce a minimal DAG ordering for the interactions so the plan is executable.\n"
            "Add ONLY necessary edges (from -> to) to encode ordering constraints.\n"
            "Use ONLY this JSON as input:\n"
            "{INTERACTION_LIST_JSON}\n\n"
            "Output JSON schema:\n"
            '{ "edges": [ [ "from_id", "to_id" ], ... ] }\n'
        )

        # Each call chains input from the previous step
        OBJECT_ANALYZER_JSON = call_openai(OBJECT_ANALYZER_PROMPT, client)

        AFFORDANCE_GENERATOR_JSON = call_openai(AFFORDANCE_GENERATOR_PROMPT.replace("{OBJECT_ANALYZER_JSON}", OBJECT_ANALYZER_JSON), client)

        AFFORDANCE_FILTER_JSON = call_openai(AFFORDANCE_FILTER_PROMPT.replace("{AFFORDANCE_LIST_JSON}", AFFORDANCE_GENERATOR_JSON), client)

        INTERACTION_GENERATOR_JSON = call_openai(INTERACTION_GENERATOR_PROMPT.replace("{FILTERED_AFFORDANCES_JSON}", AFFORDANCE_FILTER_JSON), client)

        EDGE_GENERATOR_JSON = call_openai(EDGE_GENERATOR_PROMPT.replace("{INTERACTION_LIST_JSON}", INTERACTION_GENERATOR_JSON), client)

        print("OBJECT_ANALYZER_JSON: ", OBJECT_ANALYZER_JSON)
        print("AFFORDANCE_GENERATOR_JSON: ", AFFORDANCE_GENERATOR_JSON)
        print("AFFORDANCE_FILTER_JSON: ", AFFORDANCE_FILTER_JSON)
        print("INTERACTION_GENERATOR_JSON: ", INTERACTION_GENERATOR_JSON)
        print("EDGE_GENERATOR_JSON: ", EDGE_GENERATOR_JSON)

        # Build the puzzle graph
        G = build_puzzle_graph(INTERACTION_GENERATOR_JSON, EDGE_GENERATOR_JSON)

        if nx.is_directed_acyclic_graph(G):

            try:
                G_min = nx.transitive_reduction(G)
            except Exception:
                G_min = G

            # Draw and save the graph
            safe_goal = "".join(c if c.isalnum() else "_" for c in current_goals)[:40]
            out_png = f"graph_run{type_of_run}_{safe_goal}.png"
            draw_graph(G_min, title=f"Plan DAG — {current_goals}", out_path=out_png)
            print(f"Saved graph to {out_png}")
        else:
            cycles = list(nx.simple_cycles(G))
            print("Graph is cyclic; cycles found:", cycles)

    return 0






def initialize_openai() -> OpenAI:
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        raise RuntimeError(
            "OPENAI_API_KEY is not set. Export it in your shell or .zshrc."
        )
    return OpenAI(api_key=api_key)

def call_openai(prompt: str, client: OpenAI) -> str:
    """Return JSON string from the model."""
    resp = client.chat.completions.create(
        model="gpt-5-mini",
        response_format={"type": "json_object"},
        temperature=1,
        messages=[
            {"role": "system", "content": "You are an expert at creatively connecting different objects to achieve a single goal. Given 5 different objects and a goal, you want to find a way to combine all 5 objects together in creative ways to achieve the set goal."},
            {"role": "user", "content": prompt},
        ],
    )
    return resp.choices[0].message.content


if __name__ == "__main__":
    #pipeline(1)
    pipeline(2)

