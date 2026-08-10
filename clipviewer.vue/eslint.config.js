import { defineConfig, globalIgnores } from 'eslint/config'
import globals from 'globals'
import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting'

export default defineConfig([
  {
    name: 'app/files-to-lint',
    files: ['**/*.{vue,js,mjs,jsx}'],
  },

  globalIgnores([
    '**/dist/**',
    '**/dist-ssr/**',
    '**/coverage/**',
    // Vendored shadcn-vue primitives (TypeScript source, copied verbatim from the
    // shadcn-vue registry) - not linted as part of this JS codebase.
    'src/components/ui/**',
    'src/lib/utils.ts',
  ]),

  {
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.vitest,
      },
    },
  },

  js.configs.recommended,
  ...pluginVue.configs['flat/essential'],

  skipFormatting,
  {
    rules: {
      'vue/no-deprecated-slot-attribute': 'off',
    },
  },
])
