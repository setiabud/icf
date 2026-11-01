import { createContext, useContext, type ReactNode } from 'react'
import { accountingApi, type AccountingApi } from './accountingApi'

const ApiContext = createContext<AccountingApi>(accountingApi)

export function ApiProvider({ api, children }: { api: AccountingApi; children: ReactNode }) {
  return <ApiContext.Provider value={api}>{children}</ApiContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useApi(): AccountingApi {
  return useContext(ApiContext)
}
