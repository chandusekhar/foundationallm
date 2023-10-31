import fs from 'fs';

const buildLoadingTemplate = (() => {
	const path = 'server/buildLoadingTemplate.html';

	try {
		const data = fs.readFileSync(path, 'utf8');
		return data;
	} catch (error) {
		console.error('Error reading build loading template!', error);
		return null;
	}
})();

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
	devtools: { enabled: true },
	components: true,
	app: {
		head: {
			title: process.env.BRANDING_PAGE_TITLE ?? 'FoundationaLLM',
			link: [
				{
					rel: 'icon',
					type: 'image/x-icon',
					href: process.env.BRANDING_FAV_ICON_URL ?? '/favicon.ico',
				},
			],
		},
	},
	routeRules: {
		'/': { ssr: false },
	},
	css: [
		'primevue/resources/themes/viva-light/theme.css',
		'~/styles/fonts.scss',
		'primeicons/primeicons.css',
	],
	build: {
		transpile: ['primevue'],
	},
	devServer: {
		...(buildLoadingTemplate
			? {
					loadingTemplate: () => buildLoadingTemplate,
			  }
			: {}),
	},
	vite: {
		define: {
			APP_CONFIG_ENDPOINT: JSON.stringify(process.env.APP_CONFIG_ENDPOINT),
		},
	},
});
