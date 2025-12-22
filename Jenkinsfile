pipeline {
    agent any

    environment {
        DOCKER_COMPOSE = 'docker-compose'
        WORKSPACE_DIR = "${WORKSPACE}"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Images') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose build message-sender message-receiver
                    '''
                }
            }
        }

        stage('Stop Existing Containers') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose stop rabbitmq message-sender message-receiver prometheus grafana || true
                        docker-compose rm -f rabbitmq message-sender message-receiver prometheus grafana || true
                    '''
                }
            }
        }

        stage('Start Services') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        # Ensure prometheus config file exists
                        if [ ! -f prometheus/prometheus.yml ]; then
                            echo "Error: prometheus/prometheus.yml not found"
                            exit 1
                        fi
                        # Use absolute path for prometheus config
                        PROMETHEUS_PATH="${WORKSPACE}/prometheus/prometheus.yml"
                        # Create temporary docker-compose file with absolute path
                        sed "s|\\./prometheus/prometheus\\.yml|${PROMETHEUS_PATH}|g" docker-compose.yml > docker-compose.tmp.yml
                        docker-compose -f docker-compose.tmp.yml up -d rabbitmq message-sender message-receiver prometheus grafana
                        rm -f docker-compose.tmp.yml
                    '''
                }
            }
        }

        stage('Health Check') {
            steps {
                script {
                    sleep(time: 30, unit: 'SECONDS')
                    dir("${WORKSPACE}") {
                        sh '''
                            echo "Checking services health..."
                            curl -f http://message-sender:8080/swagger || exit 1
                            curl -f http://message-receiver:8080/swagger || exit 1
                            curl -f http://prometheus:9090/-/healthy || exit 1
                            curl -f http://grafana:3000/api/health || exit 1
                            echo "All services are healthy!"
                        '''
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline completed'
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
            dir("${WORKSPACE}") {
                sh 'docker-compose logs'
            }
        }
    }
}

