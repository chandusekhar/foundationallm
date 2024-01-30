/* eslint-disable prettier/prettier */

import type {
	AgentDataSource,
	AgentIndex,
	AgentGatekeeper,
	CreateAgentRequest
} from './types';
import { mockGetAgentIndexesResponse, mockGetAgentDataSourcesResponse } from './mock';

async function wait(milliseconds: number = 1000): Promise<void> {
	return await new Promise<void>((resolve) => setTimeout(() => resolve(), milliseconds));
}

export default {
	mockLoadTime: 1000,

	apiVersion: '1.0',
	apiUrl: null,
	setApiUrl(apiUrl) {
		this.apiUrl = apiUrl;
	},

	instanceId: null,
	setInstanceId(instanceId) {
		this.instanceId = instanceId;
	},

	async getConfigValue(key: string) {
		return await $fetch(`/api/config/`, {
			params: {
				key
			}
		});
	},

	async getAgentDataSources(): Promise<AgentDataSource[]> {
		await wait(this.mockLoadTime);
		return mockGetAgentDataSourcesResponse;
	},

	async getAgentIndexes(): Promise<AgentIndex[]> {
		return JSON.parse(await $fetch(`${this.apiUrl}/instances/${this.instanceId}/providers/FoundationaLLM.Vectorization/indexingprofiles?api-version=${this.apiVersion}`));
	},

	async getAgentGatekeepers(): Promise<AgentGatekeeper[]> {
		await wait(this.mockLoadTime);
		return [];
	},

	async createAgent(request: CreateAgentRequest): Promise<void> {
		return await $fetch(`${this.apiUrl}/instances/${this.instanceId}/providers/FoundationaLLM.Agent/agents/${request.name}?api-version=${this.apiVersion}`, {
			method: 'POST',
			body: request,
		});
	},
}